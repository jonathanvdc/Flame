using System;
using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A data structure that describes the dominator tree of a
    /// control-flow graph.
    /// </summary>
    public abstract class DominatorTree
    {
        /// <summary>
        /// Gets a block's immediate dominator, that is, the block
        /// that dominates this block such that there is no intermediate
        /// block that is dominated by the immediate dominator and also
        /// dominates the given block.
        /// </summary>
        /// <param name="block">
        /// A block to find an immediate dominator for.
        /// </param>
        /// <returns>
        /// The tag of the immediate dominator block if it exists; otherwise, <c>null</c>.
        /// </returns>
        public abstract BasicBlockTag GetImmediateDominator(BasicBlockTag block);

        /// <summary>
        /// Tells if a particular block is strictly dominated by another block,
        /// that is, if control cannot flow to the block unless it first flowed
        /// through the dominator block.
        /// </summary>
        /// <param name="block">
        /// A block that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// A block that might dominate <paramref name="block"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="block"/> is strictly dominated by
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsStrictlyDominatedBy(BasicBlockTag block, BasicBlockTag dominator)
        {
            do
            {
                block = GetImmediateDominator(block);
            } while (block != null && block != dominator);
            return block == dominator;
        }

        /// <summary>
        /// Tells if a particular block is dominated by another block,
        /// that is, if control cannot flow to the block unless it first flowed
        /// through the dominator block or if the blocks are equal.
        /// </summary>
        /// <param name="block">
        /// A block that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// A block that might dominate <paramref name="block"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="block"/> is strictly dominated by
        /// <paramref name="dominator"/> or <paramref name="block"/> equals
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDominatedBy(BasicBlockTag block, BasicBlockTag dominator)
        {
            return block == dominator || IsStrictlyDominatedBy(block, dominator);
        }

        /// <summary>
        /// Tells if a particular value is strictly dominated by another value,
        /// that is, if control cannot flow to the value unless it first flowed
        /// through the dominator value.
        /// </summary>
        /// <param name="value">
        /// An value that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// An value that might dominate <paramref name="instruction"/>.
        /// </param>
        /// <param name="graph">
        /// A graph that defines both values.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is strictly dominated by
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStrictlyDominatedBy(ValueTag value, ValueTag dominator, FlowGraph graph)
        {
            var valueBlock = graph.GetValueParent(value);
            var dominatorBlock = graph.GetValueParent(dominator);
            if (valueBlock.Tag == dominatorBlock.Tag)
            {
                if (graph.ContainsBlockParameter(dominator))
                {
                    return !graph.ContainsBlockParameter(value);
                }
                else if (graph.ContainsBlockParameter(value))
                {
                    return false;
                }
                else
                {
                    var valueInsn = graph.GetInstruction(value);
                    var domInsn = graph.GetInstruction(dominator);
                    return valueInsn.InstructionIndex > domInsn.InstructionIndex;
                }
            }
            else
            {
                return IsStrictlyDominatedBy(valueBlock, dominatorBlock);
            }
        }

        /// <summary>
        /// Tells if a particular value is dominated by another value,
        /// that is, if control cannot flow to the value unless it first flowed
        /// through the dominator value.
        /// </summary>
        /// <param name="value">
        /// A value that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// A value that might dominate <paramref name="value"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is strictly dominated by
        /// <paramref name="dominator"/> or <paramref name="value"/> equals
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDominatedBy(ValueTag value, ValueTag dominator, FlowGraph graph)
        {
            return value == dominator || IsStrictlyDominatedBy(value, dominator, graph);
        }

        /// <summary>
        /// Tells if a particular value is strictly dominated by another value,
        /// that is, if control cannot flow to the value unless it first flowed
        /// through the dominator value.
        /// </summary>
        /// <param name="value">
        /// An value that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// An value that might dominate <paramref name="instruction"/>.
        /// </param>
        /// <param name="graph">
        /// A graph that defines both values.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is strictly dominated by
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStrictlyDominatedBy(ValueTag value, ValueTag dominator, FlowGraphBuilder graph)
        {
            return IsStrictlyDominatedBy(value, dominator, graph.ImmutableGraph);
        }

        /// <summary>
        /// Tells if a particular value is dominated by another value,
        /// that is, if control cannot flow to the value unless it first flowed
        /// through the dominator value.
        /// </summary>
        /// <param name="value">
        /// A value that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// A value that might dominate <paramref name="value"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is strictly dominated by
        /// <paramref name="dominator"/> or <paramref name="value"/> equals
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDominatedBy(ValueTag value, ValueTag dominator, FlowGraphBuilder graph)
        {
            return IsDominatedBy(value, dominator, graph.ImmutableGraph);
        }

        /// <summary>
        /// Tells if a particular instruction is strictly dominated by another instruction,
        /// that is, if control cannot flow to the instruction unless it first flowed
        /// through the dominator instruction.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// An instruction that might dominate <paramref name="instruction"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> is strictly dominated by
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStrictlyDominatedBy(NamedInstruction instruction, NamedInstruction dominator)
        {
            var graph = instruction.Block.Graph;
            ContractHelpers.Assert(graph == dominator.Block.Graph);
            return IsStrictlyDominatedBy(instruction, dominator, graph);
        }

        /// <summary>
        /// Tells if a particular instruction is dominated by another instruction,
        /// that is, if control cannot flow to the instruction unless it first flowed
        /// through the dominator instruction.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that might be dominated by <paramref name="dominator"/>.
        /// </param>
        /// <param name="dominator">
        /// An instruction that might dominate <paramref name="instruction"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> is strictly dominated by
        /// <paramref name="dominator"/> or <paramref name="instruction"/> equals
        /// <paramref name="dominator"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDominatedBy(NamedInstruction instruction, NamedInstruction dominator)
        {
            var graph = instruction.Block.Graph;
            ContractHelpers.Assert(graph == dominator.Block.Graph);
            return IsDominatedBy(instruction, dominator, graph);
        }
    }

    /// <summary>
    /// An analysis that computes dominator trees for control-flow graphs.
    /// </summary>
    public sealed class DominatorTreeAnalysis : IFlowGraphAnalysis<DominatorTree>
    {
        private DominatorTreeAnalysis()
        { }

        /// <summary>
        /// An instance of the dominator tree analysis.
        /// </summary>
        /// <returns>An instance of the dominator tree analysis.</returns>
        public static readonly DominatorTreeAnalysis Instance = new DominatorTreeAnalysis();

        /// <inheritdoc/>
        public DominatorTree Analyze(FlowGraph graph)
        {
            return new DominatorTreeImpl(GetImmediateDominators(graph));
        }

        /// <inheritdoc/>
        public DominatorTree AnalyzeWithUpdates(
            FlowGraph graph,
            DominatorTree previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            foreach (var item in updates)
            {
                if (item is InstructionUpdate
                    || item is BasicBlockParametersUpdate
                    || item is MapMembersUpdate)
                {
                    // These updates don't affect the dominator tree,
                    // so we can just ignore them.
                    continue;
                }
                else
                {
                    // Other instructions may affect the dominator tree,
                    // so we'll re-analyze if we encounter one.
                    return Analyze(graph);
                }
            }
            return previousResult;
        }

        /// <summary>
        /// A straightforward implementation of the DominatorTree class based
        /// on an immediate dominator mapping.
        /// </summary>
        private sealed class DominatorTreeImpl : DominatorTree
        {
            public DominatorTreeImpl(
                IReadOnlyDictionary<BasicBlockTag, BasicBlockTag> immediateDominators)
            {
                this.ImmediateDominators = immediateDominators;
            }

            public IReadOnlyDictionary<BasicBlockTag, BasicBlockTag> ImmediateDominators { get; private set; }

            public override BasicBlockTag GetImmediateDominator(BasicBlockTag block)
            {
                return ImmediateDominators[block];
            }
        }

        private static void SortPostorder(
            FlowGraph graph,
            BasicBlockTag tag,
            HashSet<BasicBlockTag> processed,
            List<BasicBlockTag> results)
        {
            if (!processed.Add(tag))
                return;

            foreach (var child in graph.GetBasicBlock(tag).Flow.BranchTargets)
            {
                SortPostorder(graph, child, processed, results);
            }

            results.Add(tag);
        }

        /// <summary>
        /// Produces a postorder traversal list for this graph, starting at the
        /// given roots.
        /// </summary>
        private static List<BasicBlockTag> SortPostorder(
            FlowGraph graph,
            IEnumerable<BasicBlockTag> roots)
        {
            var processed = new HashSet<BasicBlockTag>();
            var results = new List<BasicBlockTag>();

            foreach (var item in roots)
            {
                SortPostorder(graph, item, processed, results);
            }

            return results;
        }

        /// <summary>
        /// Produces a postorder traversal list for this graph, with the entry
        /// point as root.
        /// </summary>
        private static List<BasicBlockTag> SortPostorder(FlowGraph graph)
        {
            return SortPostorder(graph, new BasicBlockTag[] { graph.EntryPointTag });
        }

        private static BasicBlockTag IntersectImmediateDominators(
            BasicBlockTag b1, BasicBlockTag b2,
            Dictionary<BasicBlockTag, BasicBlockTag> idoms,
            Dictionary<BasicBlockTag, int> PostorderNums)
        {
            var finger1 = b1;
            var finger2 = b2;
            while (finger1 != finger2)
            {
                while (PostorderNums[finger1] < PostorderNums[finger2])
                {
                    finger1 = idoms[finger1];
                    if (finger1 == null)
                    {
                        return finger2;
                    }
                }
                    
                while (PostorderNums[finger2] < PostorderNums[finger1])
                {
                    finger2 = idoms[finger2];
                    if (finger2 == null)
                    {
                        return finger1;
                    }
                }
            }
            return finger1;
        }

        /// <summary>
        /// Computes a mapping from basic block tags to their immediate
        /// dominators. The entry point block mapped to <c>null</c>.
        /// </summary>
        private static IReadOnlyDictionary<BasicBlockTag, BasicBlockTag> GetImmediateDominators(
            FlowGraph graph)
        {
            // Based on "A Simple, Fast Dominance Algorithm" by
            // Keith D. Cooper, Timothy J. Harvey, and Ken Kennedy
            // (http://www.cs.rice.edu/~keith/Embed/dom.pdf)

            var preds = graph.GetAnalysisResult<BasicBlockPredecessors>();
            var idoms = new Dictionary<BasicBlockTag, BasicBlockTag>();
            var postorderSort = SortPostorder(graph).ToArray();
            var postorderNums = new Dictionary<BasicBlockTag, int>();
            for (int i = 0; i < postorderSort.Length; i++)
            {
                var item = postorderSort[i];
                postorderNums[item] = i;
                idoms[item] = null;
            }

            idoms[graph.EntryPointTag] = graph.EntryPointTag;

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = postorderSort.Length - 1; i >= 0; i--)
                {
                    var b = postorderSort[i];
                    if (b == graph.EntryPointTag)
                        continue;

                    BasicBlockTag newIdom = null;
                    foreach (var p in preds.GetPredecessorsOf(b))
                    {
                        if (!postorderNums.ContainsKey(p))
                            continue;

                        if (newIdom == null)
                        {
                            newIdom = p;
                        }
                        else if (idoms[p] != null)
                        {
                            newIdom = IntersectImmediateDominators(
                                p, newIdom, idoms, postorderNums);
                        }
                    }

                    if (idoms[b] != newIdom)
                    {
                        idoms[b] = newIdom;
                        changed = true;
                    }
                }
            }

            idoms[graph.EntryPointTag] = null;

            return idoms;
        }
    }
}
