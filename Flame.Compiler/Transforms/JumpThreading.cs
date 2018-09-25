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
            foreach (var block in graphBuilder.BasicBlocks)
            {
                ThreadJumps(block);
            }
            throw new System.NotImplementedException();
        }

        private void ThreadJumps(BasicBlockBuilder block)
        {
            bool changed = false;
            while (changed)
            {
                changed = false;
                var flow = block.Flow;
                if (flow is JumpFlow)
                {
                    var jumpFlow = (JumpFlow)flow;
                    var threadFlow = AsThreadableFlow(jumpFlow.Branch, block.Graph);
                    if (threadFlow != null && jumpFlow.Branch.Target != block.Tag)
                    {
                        block.Flow = threadFlow;
                        changed = true;
                    }
                }
                else if (flow is SwitchFlow && IncludeSwitches)
                {
                    var switchFlow = (SwitchFlow)flow;

                    // Rewrite switch cases.
                    var cases = new List<SwitchCase>();
                    foreach (var switchCase in switchFlow.Cases)
                    {
                        var caseFlow = AsThreadableFlow(switchCase.Branch, block.Graph);
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
                        var defaultFlow = AsThreadableFlow(defaultBranch, block.Graph);
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
                                            switchCase.Values.Intersect(valueSet),
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
        }

        private BlockFlow AsThreadableFlow(Branch branch, FlowGraphBuilder graph)
        {
            if (branch.Arguments.Count > 0)
            {
                return null;
            }

            var target = graph.GetBasicBlock(branch.Target);
            if (target.Parameters.Count > 0 && target.InstructionTags.Count > 0)
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
