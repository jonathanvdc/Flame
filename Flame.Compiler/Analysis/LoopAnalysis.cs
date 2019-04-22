using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Collections;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A data structure that maps blocks to the loops they are a part of.
    /// </summary>
    public struct BlockLoops
    {
        internal BlockLoops(IReadOnlyDictionary<BasicBlockTag, BlockLoop> blocksToLoops)
        {
            this.BlocksToLoops = blocksToLoops;
        }

        /// <summary>
        /// Gets a mapping of basic blocks to the innermost loops they are a part of.
        /// Blocks that are not in a loop don't show up in the dictionary.
        /// </summary>
        /// <value>A mapping of basic blocks to innermost loops.</value>
        public IReadOnlyDictionary<BasicBlockTag, BlockLoop> BlocksToLoops { get; private set; }
    }

    /// <summary>
    /// Represents a loop in a control-flow graph.
    /// </summary>
    public sealed class BlockLoop
    {
        internal BlockLoop(
            BlockLoop enclosingLoop,
            ImmutableHashSet<BasicBlockTag> body,
            ImmutableHashSet<BasicBlockTag> entries,
            ImmutableHashSet<BasicBlockTag> exits)
        {
            this.EnclosingLoopOrNull = enclosingLoop;
            this.Body = body;
            this.Entries = entries;
            this.Exits = exits;
        }

        /// <summary>
        /// Gets this loop's enclosing loop, if any.
        /// </summary>
        /// <value>The enclosing loop if there is one; otherwise, <c>null</c>.</value>
        public BlockLoop EnclosingLoopOrNull { get; private set; }

        /// <summary>
        /// Tells if this block loop has an enclosing loop.
        /// </summary>
        public bool IsNestedLoop => EnclosingLoopOrNull != null;

        /// <summary>
        /// Gets the set of all blocks in this loop's body.
        /// </summary>
        /// <value>The set of all blocks in this loop's body.</value>
        public ImmutableHashSet<BasicBlockTag> Body { get; private set; }

        /// <summary>
        /// Gets the set of all blocks outside of this loop's body that
        /// have a branch into this loop's body.
        /// </summary>
        /// <value>The set of all entry blocks.</value>
        public ImmutableHashSet<BasicBlockTag> Entries { get; private set; }

        /// <summary>
        /// Gets the set of all blocks inside of this loop's body that
        /// have a branch out of this loop's body.
        /// </summary>
        /// <value>The set of all exit blocks.</value>
        public ImmutableHashSet<BasicBlockTag> Exits { get; private set; }
    }

    /// <summary>
    /// An analysis that finds loop in control-flow graphs.
    /// </summary>
    public sealed class LoopAnalysis : IFlowGraphAnalysis<BlockLoops>
    {
        private LoopAnalysis()
        { }

        /// <summary>
        /// An instance of the loop analysis.
        /// </summary>
        /// <value>A loop analysis instance.</value>
        public static readonly LoopAnalysis Instance = new LoopAnalysis();

        /// <inheritdoc/>
        public BlockLoops Analyze(FlowGraph graph)
        {
            var results = new Dictionary<BasicBlockTag, BlockLoop>();
            FindLoops(new HashSet<BasicBlockTag>(graph.BasicBlockTags), graph, null, results);
            return new BlockLoops(results);
        }

        private static void FindLoops(
            HashSet<BasicBlockTag> component,
            FlowGraph graph,
            BlockLoop parent,
            Dictionary<BasicBlockTag, BlockLoop> blocksToLoops)
        {
            var preds = graph.GetAnalysisResult<BasicBlockPredecessors>();
            foreach (var scc in StronglyConnectedComponents.Compute(
                component,
                tag => graph.GetBasicBlock(tag).Flow.BranchTargets.Where(component.Contains)))
            {
                if (scc.Count == 1)
                {
                    var singleBlock = graph.GetBasicBlock(scc.Single());
                    if (!graph.GetBasicBlock(singleBlock).Flow.BranchTargets.Contains(singleBlock))
                    {
                        // Don't include trivial SCCs that aren't real loops.
                        continue;
                    }
                }

                var entries = ImmutableHashSet.CreateRange(
                    scc.SelectMany(preds.GetPredecessorsOf).Except(scc));

                var exits = ImmutableHashSet.CreateRange(
                    scc.Select(graph.GetBasicBlock)
                        .SelectMany(block => block.Flow.BranchTargets)
                        .Except(scc));

                // Create the loop.
                var loop = new BlockLoop(parent, ImmutableHashSet.CreateRange(scc), entries, exits);

                // Update the loop's body.
                foreach (var tag in scc)
                {
                    blocksToLoops[tag] = loop;
                }

                // Now look for nested loops.
                FindLoops(scc, graph, loop, blocksToLoops);
            }
        }

        /// <inheritdoc/>
        public BlockLoops AnalyzeWithUpdates(
            FlowGraph graph,
            BlockLoops previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
