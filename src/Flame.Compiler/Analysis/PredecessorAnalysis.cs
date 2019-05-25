using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A data structure that can be queried to get the predecessors
    /// of a block, that is, the set of all blocks that have branches
    /// to the block.
    /// </summary>
    public struct BasicBlockPredecessors
    {
        internal BasicBlockPredecessors(Dictionary<BasicBlockTag, HashSet<BasicBlockTag>> values)
        {
            this.predecessorDict = values;
        }

        private Dictionary<BasicBlockTag, HashSet<BasicBlockTag>> predecessorDict;

        /// <summary>
        /// Gets the set of all predecessors of a basic block with a particular tag.
        /// </summary>
        /// <param name="block">The tag of the basic block to examine.</param>
        /// <returns>A set of predecessors.</returns>
        public IEnumerable<BasicBlockTag> GetPredecessorsOf(BasicBlockTag block)
        {
            return predecessorDict[block];
        }

        /// <summary>
        /// Tests if one block is a predecessor of another.
        /// </summary>
        /// <param name="potentialPredecessor">
        /// The tag of a block that might be a predecessor of <paramref name="block"/>,
        /// that is, the block to examine for predecessorness here.
        /// </param>
        /// <param name="block">
        /// The tag of a basic block in the flow graph.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="potentialPredecessor"/> is a predecessor of
        /// <paramref name="block"/>; otherwise; <c>false</c>.
        /// </returns>
        public bool IsPredecessorOf(BasicBlockTag potentialPredecessor, BasicBlockTag block)
        {
            return predecessorDict[block].Contains(potentialPredecessor);
        }
    }

    /// <summary>
    /// An analysis that finds basic block predecessors.
    /// </summary>
    public sealed class PredecessorAnalysis : IFlowGraphAnalysis<BasicBlockPredecessors>
    {
        private PredecessorAnalysis()
        { }

        /// <summary>
        /// An instance of the basic block predecessor analysis.
        /// </summary>
        public static readonly PredecessorAnalysis Instance = new PredecessorAnalysis();

        /// <inheritdoc/>
        public BasicBlockPredecessors Analyze(FlowGraph graph)
        {
            var predecessorDict = new Dictionary<BasicBlockTag, HashSet<BasicBlockTag>>();

            // Fill the predecessor dictionary with empty sets.
            foreach (var tag in graph.BasicBlockTags)
            {
                predecessorDict[tag] = new HashSet<BasicBlockTag>();
            }

            // Fill the sets.
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var branch in block.Flow.Branches)
                {
                    predecessorDict[branch.Target].Add(block.Tag);
                }
            }

            return new BasicBlockPredecessors(predecessorDict);
        }

        /// <inheritdoc/>
        public BasicBlockPredecessors AnalyzeWithUpdates(
            FlowGraph graph,
            BasicBlockPredecessors previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            foreach (var item in updates)
            {
                if (item is InstructionUpdate
                    || item is BasicBlockParametersUpdate
                    || item is SetEntryPointUpdate
                    || item is MapMembersUpdate)
                {
                    // These updates don't affect the predecessor analysis,
                    // so we can just ignore them.
                    continue;
                }
                else
                {
                    // Other instructions may affect the predecessor analysis,
                    // so we'll re-analyze if we encounter one.
                    return Analyze(graph);
                }
            }
            return previousResult;
        }
    }
}
