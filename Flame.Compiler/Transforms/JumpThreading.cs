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

            // Suggest exception spec analyses if the graph doesn't have any yet.
            if (!graphBuilder.HasAnalysisFor<PrototypeExceptionSpecs>())
            {
                graphBuilder.AddAnalysis(
                    new ConstantAnalysis<PrototypeExceptionSpecs>(
                        RuleBasedPrototypeExceptionSpecs.Default));
            }
            if (!graphBuilder.HasAnalysisFor<InstructionExceptionSpecs>())
            {
                graphBuilder.AddAnalysis(
                    new ConstantAnalysis<InstructionExceptionSpecs>(
                        new TrivialInstructionExceptionSpecs(
                            graphBuilder.GetAnalysisResult<PrototypeExceptionSpecs>())));
            }

            var finished = new HashSet<BasicBlockTag>();
            foreach (var block in graphBuilder.BasicBlocks)
            {
                ThreadJumps(block, finished);
            }
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
                }
            }
            else if (flow is TryFlow)
            {
                // We might be able to turn try flow into a jump, which we can thread
                // recursively.

                var tryFlow = (TryFlow)flow;
                var exceptionSpecs = block.Graph.GetAnalysisResult<InstructionExceptionSpecs>();
                if (!exceptionSpecs.GetExceptionSpecification(tryFlow.Instruction).CanThrowSomething)
                {
                    // If the "risky" instruction absolutely cannot throw,
                    // then we can just rewrite the try as a jump.
                    var value = block.AppendInstruction(tryFlow.Instruction);
                    var branch = tryFlow.SuccessBranch.WithArguments(
                        tryFlow.SuccessBranch.Arguments
                            .Select(arg => arg.IsTryResult ? BranchArgument.FromValue(value) : arg)
                            .ToArray());
                    block.Flow = new JumpFlow(branch);

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

                    var capturedExceptionParam = tryFlow.ExceptionBranch
                        .ZipArgumentsWithParameters(block.Graph)
                        .FirstOrDefault(pair => pair.Value.IsTryException)
                        .Key;

                    if (capturedExceptionParam == null)
                    {
                        // If the exception branch does not pass an '#exception' parameter,
                        // then we can replace the try by a simple jump.
                        block.Flow = new JumpFlow(tryFlow.ExceptionBranch);
                    }
                    else
                    {
                        // Otherwise, we actually need to capture the exception.
                        var capturedException = block.AppendInstruction(
                            Instruction.CreateCaptureIntrinsic(
                                block.Graph.GetValueType(capturedExceptionParam),
                                block.Graph.GetValueType(exception),
                                exception));
                        var branch = tryFlow.ExceptionBranch.WithArguments(
                            tryFlow.ExceptionBranch.Arguments
                                .Select(arg => arg.IsTryException ? BranchArgument.FromValue(capturedException) : arg)
                                .ToArray());

                        block.Flow = new JumpFlow(branch);
                    }

                    // Jump-thread this block again.
                    processedBlocks.Remove(block);
                    ThreadJumps(block, processedBlocks);
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
            }
        }

        private BlockFlow AsThreadableFlow(Branch branch, FlowGraphBuilder graph, HashSet<BasicBlockTag> processedBlocks)
        {
            ThreadJumps(graph.GetBasicBlock(branch.Target), processedBlocks);

            if (branch.Arguments.Count > 0)
            {
                return null;
            }

            var target = graph.GetBasicBlock(branch.Target);
            if (target.Parameters.Count > 0 || target.InstructionTags.Count > 0)
            {
                return null;
            }

            if (target.Flow is JumpFlow || target.Flow is SwitchFlow)
            {
                return target.Flow;
            }
            else
            {
                return null;
            }
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
    }
}
