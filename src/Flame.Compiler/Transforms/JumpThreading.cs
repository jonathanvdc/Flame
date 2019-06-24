using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that tries to eliminate repeated
    /// jumps between blocks.
    /// </summary>
    public sealed class JumpThreading : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates an instance of the jump threading transform.
        /// </summary>
        /// <param name="includeSwitches">
        /// Tells if switches are also eligible for jump threading.
        /// If set to <c>true</c>, then switches that branch to
        /// other switches can be merged and jumps to switches can
        /// be replaced with the switch itself.
        /// </param>
        public JumpThreading(bool includeSwitches = true)
        {
            this.IncludeSwitches = includeSwitches;
        }

        /// <summary>
        /// Tells if switches are also eligible for jump threading.
        /// If set to <c>true</c>, then switches that branch to
        /// other switches can be merged and jumps to switches can
        /// be replaced with the switch itself.
        /// </summary>
        /// <value>
        /// <c>true</c> if switches are eligible for jump threading; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeSwitches { get; private set; }

        /// <summary>
        /// Applies the jump threading optimization to a flow graph.
        /// </summary>
        /// <param name="graph">The flow graph to rewrite.</param>
        /// <returns>An optimized flow graph.</returns>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var graphBuilder = graph.ToBuilder();

            // Ensure that the graph is in register forwarding form.
            // Jump threading will work even if it isn't, but register
            // forwarding form will allow jump threading to make more
            // aggressive simplifications.
            graphBuilder.Transform(ForwardRegisters.Instance);

            var finished = new HashSet<BasicBlockTag>();
            foreach (var block in graphBuilder.BasicBlocks)
            {
                ThreadJumps(block, finished);
            }

            // Move out of register forwarding form by applying copy propagation
            // and dead code elimination.
            graphBuilder.Transform(CopyPropagation.Instance, DeadValueElimination.Instance, DeadBlockElimination.Instance);

            return graphBuilder.ToImmutable();
        }

        private void ThreadJumps(BasicBlockBuilder block, HashSet<BasicBlockTag> processedBlocks)
        {
            if (!processedBlocks.Add(block))
            {
                return;
            }

            var flow = block.Flow;
            if (flow is JumpFlow)
            {
                // Jump flow is usually fairly straightforward to thread.
                var jumpFlow = (JumpFlow)flow;
                var threadFlow = AsThreadableFlow(jumpFlow.Branch, block.Graph, processedBlocks);
                if (threadFlow != null && jumpFlow.Branch.Target != block.Tag)
                {
                    block.Flow = threadFlow;

                    if (block.Flow is SwitchFlow)
                    {
                        // If the jump flow gets replaced by switch flow, then we want to
                        // jump-thread the block again.
                        processedBlocks.Remove(block);
                        ThreadJumps(block, processedBlocks);
                        return;
                    }
                }

                // Now would also be a good time to try and fuse this block with
                // its successor, if this jump is the only branch to the successor.
                var predecessors = block.Graph.GetAnalysisResult<BasicBlockPredecessors>();
                BasicBlockTag tail;
                if (BlockFusion.TryGetFusibleTail(block, block.Graph.ToImmutable(), predecessors, out tail))
                {
                    // Fuse the blocks.
                    FuseBlocks(block, tail);

                    // Fusing these blocks may have exposed additional information
                    // that will enable us to thread more jumps.
                    processedBlocks.Remove(block);
                    ThreadJumps(block, processedBlocks);
                }
            }
            else if (flow is TryFlow)
            {
                // We might be able to turn try flow into a jump, which we can thread
                // recursively.
                var tryFlow = (TryFlow)flow;

                // Start off by threading the try flow's branches.
                var successBranch = tryFlow.SuccessBranch;
                var failureBranch = tryFlow.ExceptionBranch;
                var successFlow = AsThreadableFlow(tryFlow.SuccessBranch, block.Graph, processedBlocks);
                var failureFlow = AsThreadableFlow(tryFlow.ExceptionBranch, block.Graph, processedBlocks);
                if (successFlow is JumpFlow)
                {
                    successBranch = ((JumpFlow)successFlow).Branch;
                }
                if (failureFlow is JumpFlow)
                {
                    failureBranch = ((JumpFlow)failureFlow).Branch;
                }

                var exceptionSpecs = block.Graph.GetAnalysisResult<InstructionExceptionSpecs>();
                if (!exceptionSpecs.GetExceptionSpecification(tryFlow.Instruction).CanThrowSomething
                    || IsRethrowBranch(failureBranch, block.Graph, processedBlocks))
                {
                    // If the "risky" instruction absolutely cannot throw, then we can
                    // just rewrite the try as a jump.
                    // Similarly, if the exception branch simply re-throws the exception,
                    // then we are also free to make the same transformation, but for
                    // different reasons.
                    var value = block.AppendInstruction(tryFlow.Instruction);
                    var branch = successBranch.WithArguments(
                        successBranch.Arguments
                            .Select(arg => arg.IsTryResult ? BranchArgument.FromValue(value) : arg)
                            .ToArray());
                    block.Flow = new JumpFlow(branch);

                    // Jump-thread this block again.
                    processedBlocks.Remove(block);
                    ThreadJumps(block, processedBlocks);
                }
                else if (IsRethrowIntrinsic(tryFlow.Instruction))
                {
                    // If the "risky" instruction is really just a 'rethrow' intrinsic,
                    // then we can branch directly to the exception branch.

                    var capturedException = tryFlow.Instruction.Arguments[0];
                    JumpToExceptionBranch(block, failureBranch, capturedException);

                    // Jump-thread this block again.
                    processedBlocks.Remove(block);
                    ThreadJumps(block, processedBlocks);
                }
                else if (IsThrowIntrinsic(tryFlow.Instruction))
                {
                    // If the "risky" instruction is really just a 'throw' intrinsic,
                    // then we can insert an instruction to capture the exception and
                    // jump directly to the exception block.
                    var exception = tryFlow.Instruction.Arguments[0];

                    var capturedExceptionParam = failureBranch
                        .ZipArgumentsWithParameters(block.Graph)
                        .FirstOrDefault(pair => pair.Value.IsTryException)
                        .Key;

                    if (capturedExceptionParam == null)
                    {
                        // If the exception branch does not pass an '#exception' parameter,
                        // then we can replace the try by a simple jump.
                        block.Flow = new JumpFlow(failureBranch);
                    }
                    else
                    {
                        // Otherwise, we actually need to capture the exception.
                        var capturedException = block.AppendInstruction(
                            Instruction.CreateCaptureIntrinsic(
                                block.Graph.GetValueType(capturedExceptionParam),
                                block.Graph.GetValueType(exception),
                                exception));

                        JumpToExceptionBranch(block, failureBranch, capturedException);
                    }

                    // Jump-thread this block again.
                    processedBlocks.Remove(block);
                    ThreadJumps(block, processedBlocks);
                }
                else
                {
                    // If we can't replace the 'try' flow with something better, then
                    // the least we can do is try and thread the 'try' flow's branches.
                    block.Flow = new TryFlow(tryFlow.Instruction, successBranch, failureBranch);
                }
            }
            else if (flow is SwitchFlow && IncludeSwitches)
            {
                // If we're allowed to, then we might be able to thread jumps in switch flow
                // branches.
                bool changed = false;
                var switchFlow = (SwitchFlow)flow;

                // Rewrite switch cases.
                var cases = new List<SwitchCase>();
                foreach (var switchCase in switchFlow.Cases)
                {
                    var caseFlow = AsThreadableFlow(switchCase.Branch, block.Graph, processedBlocks);
                    if (caseFlow != null
                        && switchCase.Branch.Target != block.Tag)
                    {
                        if (caseFlow is JumpFlow)
                        {
                            cases.Add(
                                new SwitchCase(
                                    switchCase.Values,
                                    ((JumpFlow)caseFlow).Branch));
                            changed = true;
                            continue;
                        }
                        else if (caseFlow is SwitchFlow)
                        {
                            var threadedSwitch = (SwitchFlow)caseFlow;
                            if (threadedSwitch.SwitchValue == switchFlow.SwitchValue)
                            {
                                var valuesToBranches = threadedSwitch.ValueToBranchMap;
                                foreach (var value in switchCase.Values)
                                {
                                    Branch branchForValue;
                                    if (!valuesToBranches.TryGetValue(value, out branchForValue))
                                    {
                                        branchForValue = threadedSwitch.DefaultBranch;
                                    }
                                    cases.Add(
                                        new SwitchCase(
                                            ImmutableHashSet.Create(value),
                                            branchForValue));
                                }
                                changed = true;
                                continue;
                            }
                        }
                    }
                    cases.Add(switchCase);
                }

                // Rewrite default branch if possible.
                var defaultBranch = switchFlow.DefaultBranch;
                if (defaultBranch.Target != block.Tag)
                {
                    var defaultFlow = AsThreadableFlow(defaultBranch, block.Graph, processedBlocks);
                    if (defaultFlow is JumpFlow)
                    {
                        defaultBranch = ((JumpFlow)defaultFlow).Branch;
                        changed = true;
                    }
                    else if (defaultFlow is SwitchFlow)
                    {
                        var threadedSwitch = (SwitchFlow)defaultFlow;
                        if (threadedSwitch.SwitchValue == switchFlow.SwitchValue)
                        {
                            var valueSet = ImmutableHashSet.CreateRange(switchFlow.ValueToBranchMap.Keys);
                            foreach (var switchCase in threadedSwitch.Cases)
                            {
                                cases.Add(
                                    new SwitchCase(
                                        switchCase.Values.Except(valueSet),
                                        switchCase.Branch));
                            }
                            defaultBranch = threadedSwitch.DefaultBranch;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    block.Flow = new SwitchFlow(
                        switchFlow.SwitchValue,
                        cases,
                        defaultBranch);
                }

                // Also simplify the block's switch flow. If the switch
                // flow decays to jump flow, then we want to try threading
                // this block again.
                if (SwitchSimplification.TrySimplifySwitchFlow(block)
                    && block.Flow is JumpFlow)
                {
                    processedBlocks.Remove(block);
                    ThreadJumps(block, processedBlocks);
                }
            }
        }

        /// <summary>
        /// Replaces a block's flow with an unconditional jump to an exception
        /// branch. Replaces 'try' flow exception arguments in that exception
        /// branch with a captured exception value.
        /// </summary>
        /// <param name="block">The block to rewrite.</param>
        /// <param name="exceptionBranch">The exception branch to jump to unconditionally.</param>
        /// <param name="capturedException">
        /// The captured exception to pass instead of a 'try' flow exception argument.
        /// </param>
        private static void JumpToExceptionBranch(
            BasicBlockBuilder block,
            Branch exceptionBranch,
            ValueTag capturedException)
        {
            var branch = exceptionBranch.WithArguments(
                exceptionBranch.Arguments
                    .Select(arg => arg.IsTryException ? BranchArgument.FromValue(capturedException) : arg)
                    .ToArray());

            block.Flow = new JumpFlow(branch);
        }

        private bool IsRethrowBranch(Branch exceptionBranch, FlowGraphBuilder graph, HashSet<BasicBlockTag> processedBlocks)
        {
            // Jump-thread the exception branch's target so we will have more
            // information to act on.
            var target = graph.GetBasicBlock(exceptionBranch.Target);
            ThreadJumps(target, processedBlocks);

            // Now walk the exception branch's instructions. If the first effectful
            // instruction is a 'rethrow' intrinsic, then we are dealing with a
            // rethrow branch.
            var effectfulness = graph.GetAnalysisResult<EffectfulInstructions>();
            foreach (var instruction in target.NamedInstructions)
            {
                if (effectfulness.Instructions.Contains(instruction))
                {
                    if (IsRethrowIntrinsic(instruction.Instruction))
                    {
                        return exceptionBranch
                            .ZipArgumentsWithParameters(graph)
                            .Where(pair => pair.Value.IsTryException)
                            .Select(pair => pair.Key)
                            .Any(instruction.Arguments.Contains);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return false;
        }

        private BlockFlow AsThreadableFlow(Branch branch, FlowGraphBuilder graph, HashSet<BasicBlockTag> processedBlocks)
        {
            ThreadJumps(graph.GetBasicBlock(branch.Target), processedBlocks);

            // Block arguments are a bit of an obstacle for jump threading.
            //
            //   * We can easily thread jumps to blocks that have block parameters
            //     if those parameters are never used outside of the block: in that case,
            //     we just substitute arguments for parameters.
            //
            //   * Jumps to blocks that have block parameters that are used outside
            //     of the block that defines them are trickier to handle. We'll just bail
            //     when we encounter them, because these don't occur when the control-flow
            //     graph is in register forwarding form.

            var target = graph.GetBasicBlock(branch.Target);
            var uses = graph.GetAnalysisResult<ValueUses>();

            // Only jump and switch flow are threadable. We don't jump-thread through
            // instructions.
            // TODO: consider copying instructions to enable more aggressive jump threading.
            if ((!(target.Flow is JumpFlow) && !(target.Flow is SwitchFlow))
                || target.InstructionTags.Count > 0
                || target.ParameterTags.Any(
                    tag => uses.GetInstructionUses(tag).Count > 0
                        || uses.GetFlowUses(tag).Any(block => block != target.Tag)))
            {
                return null;
            }

            var valueSubstitutionMap = new Dictionary<ValueTag, ValueTag>();
            var argSubstitutionMap = new Dictionary<BranchArgument, BranchArgument>();
            foreach (var kvPair in branch.ZipArgumentsWithParameters(graph))
            {
                if (kvPair.Value.IsValue)
                {
                    valueSubstitutionMap[kvPair.Key] = kvPair.Value.ValueOrNull;
                }
                argSubstitutionMap[BranchArgument.FromValue(kvPair.Key)] = kvPair.Value;
            }
            return target.Flow
                .MapArguments(argSubstitutionMap)
                .MapValues(valueSubstitutionMap);
        }

        /// <summary>
        /// Fuses two fusible basic blocks.
        /// </summary>
        /// <param name="head">The 'head' block.</param>
        /// <param name="tail">The 'tail' block.</param>
        private static void FuseBlocks(BasicBlockBuilder head, BasicBlockTag tail)
        {
            var jump = (JumpFlow)head.Flow;

            // Replace branch parameters by their respective arguments.
            var replacements = new Dictionary<ValueTag, ValueTag>();
            foreach (var pair in jump.Branch.ZipArgumentsWithParameters(head.Graph))
            {
                replacements.Add(pair.Key, pair.Value.ValueOrNull);
            }
            head.Graph.ReplaceUses(replacements);

            // Move instructions around.
            var tailBlock = head.Graph.GetBasicBlock(tail);
            foreach (var instruction in tailBlock.NamedInstructions)
            {
                instruction.MoveTo(head);
            }

            // Update the block's flow.
            head.Flow = tailBlock.Flow;

            // Delete the 'tail' block.
            head.Graph.RemoveBasicBlock(tail);
        }

        /// <summary>
        /// Tells if an instruction is a 'throw' intrinsic.
        /// </summary>
        /// <param name="instruction">The instruction to examine.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> is a 'throw' intrinsic; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsThrowIntrinsic(Instruction instruction)
        {
            return ExceptionIntrinsics.Namespace.IsIntrinsicPrototype(
                instruction.Prototype,
                ExceptionIntrinsics.Operators.Throw);
        }

        /// <summary>
        /// Tells if an instruction is a 'rethrow' intrinsic.
        /// </summary>
        /// <param name="instruction">The instruction to examine.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> is a 'rethrow' intrinsic; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsRethrowIntrinsic(Instruction instruction)
        {
            return ExceptionIntrinsics.Namespace.IsIntrinsicPrototype(
                instruction.Prototype,
                ExceptionIntrinsics.Operators.Rethrow);
        }
    }
}
