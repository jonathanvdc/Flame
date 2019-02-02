using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Compiler.Flow;

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
                // TODO: switch to an actual analysis for exception specifications.

                var tryFlow = (TryFlow)flow;
                if (!tryFlow.Instruction.Prototype.ExceptionSpecification.CanThrowSomething)
                {
                    // Rewrite the try as a jump.
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
    }
}
