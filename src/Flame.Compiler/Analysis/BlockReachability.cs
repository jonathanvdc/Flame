using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A description of which blocks can reach other blocks in a
    /// control-flow graph.
    /// </summary>
    public abstract class BlockReachability
    {
        /// <summary>
        /// Tests if there exists a nonempty path through the
        /// control-flow graph that starts at <paramref name="source"/>
        /// and ends at <paramref name="target"/>.
        /// </summary>
        /// <param name="source">
        /// The block tag of the start of the path.
        /// </param>
        /// <param name="target">
        /// The block tag of the end of the path.
        /// </param>
        /// <returns>
        /// <c>true</c> if there is such a path; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsStrictlyReachableFrom(BasicBlockTag source, BasicBlockTag target);

        /// <summary>
        /// Tests if there exists a (possibly empty) path through the
        /// control-flow graph that starts at <paramref name="source"/>
        /// and ends at <paramref name="target"/>.
        /// </summary>
        /// <param name="source">
        /// The block tag of the start of the path.
        /// </param>
        /// <param name="target">
        /// The block tag of the end of the path.
        /// </param>
        /// <returns>
        /// <c>true</c> if there is such a path; otherwise, <c>false</c>.
        /// </returns>
        public bool IsReachableFrom(BasicBlockTag source, BasicBlockTag target)
        {
            return source == target || IsStrictlyReachableFrom(source, target);
        }

        /// <summary>
        /// Gets the set of all basic blocks that are strictly reachable from a
        /// particular basic block.
        /// </summary>
        /// <param name="source">The source block to start at.</param>
        /// <returns>
        /// A set of basic block tags referring to basic blocks that
        /// are reachable from <paramref name="source"/>.
        /// </returns>
        public abstract IEnumerable<BasicBlockTag> GetStrictlyReachableBlocks(BasicBlockTag source);
    }

    /// <summary>
    /// A block reachability implementation that performs reachability
    /// analysis on an on-demand basis and caches the results.
    /// </summary>
    public sealed class LazyBlockReachability : BlockReachability
    {
        /// <summary>
        /// Creates a lazy block reachability analysis for a particular graph.
        /// </summary>
        /// <param name="graph">The graph to create a reachability analysis for.</param>
        public LazyBlockReachability(FlowGraph graph)
        {
            this.Graph = graph;
            this.results = new Dictionary<BasicBlockTag, HashSet<BasicBlockTag>>();
        }

        /// <summary>
        /// Gets the flow graph that is queried for reachability.
        /// </summary>
        /// <value>The flow graph.</value>
        public FlowGraph Graph { get; private set; }

        private Dictionary<BasicBlockTag, HashSet<BasicBlockTag>> results;

        /// <inheritdoc/>
        public override IEnumerable<BasicBlockTag> GetStrictlyReachableBlocks(BasicBlockTag source)
        {
            return GetReachableBlocksImpl(source);
        }

        /// <inheritdoc/>
        public override bool IsStrictlyReachableFrom(BasicBlockTag source, BasicBlockTag target)
        {
            return GetReachableBlocksImpl(source).Contains(target);
        }

        private HashSet<BasicBlockTag> GetReachableBlocksImpl(BasicBlockTag source)
        {
            HashSet<BasicBlockTag> reachable;
            if (results.TryGetValue(source, out reachable))
            {
                return reachable;
            }
            else
            {
                reachable = new HashSet<BasicBlockTag>();
                AddReachableBlocks(source, reachable);
                results[source] = reachable;
                return reachable;
            }
        }

        private void AddReachableBlocks(
            BasicBlockTag source,
            HashSet<BasicBlockTag> reachable)
        {
            // Try to re-use existing results if possible.
            HashSet<BasicBlockTag> existingReachability;
            if (results.TryGetValue(source, out existingReachability))
            {
                reachable.UnionWith(existingReachability);
            }
            else
            {
                foreach (var branch in Graph.GetBasicBlock(source).Flow.Branches)
                {
                    if (reachable.Add(branch.Target))
                    {
                        AddReachableBlocks(branch.Target, reachable);
                    }
                }
            }
        }
    }

    /// <summary>
    /// An analysis that finds computes block reachability information
    /// on an on-demand basis.
    /// </summary>
    public sealed class LazyBlockReachabilityAnalysis : IFlowGraphAnalysis<LazyBlockReachability>
    {
        private LazyBlockReachabilityAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the lazy block reachability analysis.
        /// </summary>
        /// <returns>An instance of the lazy block reachability analysis.</returns>
        public static readonly LazyBlockReachabilityAnalysis Instance = new LazyBlockReachabilityAnalysis();

        /// <inheritdoc/>
        public LazyBlockReachability Analyze(FlowGraph graph)
        {
            return new LazyBlockReachability(graph);
        }

        /// <inheritdoc/>
        public LazyBlockReachability AnalyzeWithUpdates(
            FlowGraph graph,
            LazyBlockReachability previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            foreach (var item in updates)
            {
                if (item is InstructionUpdate
                    || item is BasicBlockParametersUpdate
                    || item is SetEntryPointUpdate
                    || item is MapMembersUpdate)
                {
                    // These updates don't affect the reachability analysis,
                    // so we can just ignore them.
                    continue;
                }
                else
                {
                    // Other instructions may affect the reachability analysis,
                    // so we'll re-analyze if we encounter one.
                    return Analyze(graph);
                }
            }
            return previousResult;
        }
    }
}
