using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Contains value liveness data for a single block.
    /// </summary>
    public sealed class BlockLiveness
    {
        /// <summary>
        /// Creates empty block liveness data for a block.
        /// </summary>
        /// <param name="block">
        /// The block for which this liveness data.
        /// </param>
        private BlockLiveness(BasicBlock block)
        {
            this.block = block;
            this.blockInstructions = new HashSet<ValueTag>(block.InstructionTags);
            this.livePositions = new Dictionary<ValueTag, int>();
            this.deadPositions = new Dictionary<ValueTag, int>();
            this.ExportIndex = block.InstructionTags.Count
                + block.Flow.Instructions.Count
                + block.Flow.Branches.Count;
        }

        private BasicBlock block;

        /// <summary>
        /// The index of imported values in the virtual instruction list.
        /// </summary>
        public const int ImportIndex = -2;

        /// <summary>
        /// The index of parameters in the virtual instruction list.
        /// </summary>
        public const int ParameterIndex = -1;

        /// <summary>
        /// The index of exported values in the virtual instruction list.
        /// </summary>
        public int ExportIndex { get; private set; }

        /// <summary>
        /// The set of all instruction tags in the block.
        /// </summary>
        private HashSet<ValueTag> blockInstructions;

        /// <summary>
        /// A mapping of value tags to the instruction indices after which
        /// they become live.
        /// </summary>
        private Dictionary<ValueTag, int> livePositions;

        /// <summary>
        /// A mapping of value tags to the instruction indices after which
        /// they become dead.
        /// </summary>
        private Dictionary<ValueTag, int> deadPositions;

        /// <summary>
        /// Gets this block's sequence of imported values.
        /// </summary>
        /// <value>A sequence of imported values.</value>
        public IEnumerable<ValueTag> Imports
        {
            get
            {
                return livePositions
                    .Where(pair => pair.Value == ImportIndex)
                    .Select(pair => pair.Key);
            }
        }

        /// <summary>
        /// Tells if a value is live right after a particular instruction in this block.
        /// </summary>
        /// <param name="value">
        /// The value whose liveness is to be queried.
        /// </param>
        /// <param name="index">
        /// An index into the instruction list that defines the point at
        /// which the value's liveness is queried.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is live right after <paramref name="index"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsLiveAt(ValueTag value, int index)
        {
            if (livePositions.ContainsKey(value))
            {
                return livePositions[value] < index && index < deadPositions[value];
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tells if a value is directly defined by this block.
        /// </summary>
        /// <param name="value">The value that may or may not be defined.</param>
        /// <returns>
        /// <c>true</c> if the value is defined by this block; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDefined(ValueTag value)
        {
            return livePositions.ContainsKey(value)
                && livePositions[value] >= ParameterIndex;
        }

        /// <summary>
        /// Tells if a value is imported by this block.
        /// </summary>
        /// <param name="value">The value that may or may not be imported.</param>
        /// <returns>
        /// <c>true</c> if the value is imported; otherwise, <c>false</c>.
        /// </returns>
        public bool IsImported(ValueTag value)
        {
            return livePositions.ContainsKey(value)
                && livePositions[value] == ImportIndex;
        }

        /// <summary>
        /// Tells if a value is either defined or imported by this block.
        /// </summary>
        /// <param name="value">
        /// The value that may or may not be defined or imported by this block.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value is defined or imported; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDefinedOrImported(ValueTag value)
        {
            return livePositions.ContainsKey(value);
        }

        /// <summary>
        /// Gets a mapping of virtual instruction indices to the values that are
        /// live at those indices.
        /// </summary>
        /// <returns>A mapping of virtual instruction indices to live values.</returns>
        public IReadOnlyDictionary<int, ImmutableHashSet<ValueTag>> GetLiveValuesByIndex()
        {
            var liveByIndex = GroupByValue(livePositions);
            var deadByIndex = GroupByValue(deadPositions);

            var results = new Dictionary<int, ImmutableHashSet<ValueTag>>();
            var liveSet = ImmutableHashSet.Create<ValueTag>();
            for (int i = ImportIndex; i <= ExportIndex; i++)
            {
                if (liveByIndex.ContainsKey(i))
                {
                    liveSet = liveSet.Union(liveByIndex[i]);
                }
                if (deadByIndex.ContainsKey(i))
                {
                    liveSet = liveSet.Except(deadByIndex[i]);
                }
                results[i] = liveSet;
            }
            return results;
        }

        private static Dictionary<TValue, HashSet<TKey>> GroupByValue<TKey, TValue>(
            IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return pairs
                .GroupBy(pair => pair.Value, pair => pair.Key)
                .ToDictionary(group => group.Key, group => new HashSet<TKey>(group));
        }

        /// <summary>
        /// Adds a particular value to this block's list of imports.
        /// </summary>
        /// <param name="value">The imported value.</param>
        /// <returns>
        /// <c>true</c> if the value is newly imported; otherwise, <c>false</c>.
        /// </returns>
        internal bool Import(ValueTag value)
        {
            if (IsImported(value))
            {
                return false;
            }

            livePositions[value] = ImportIndex;
            if (!deadPositions.ContainsKey(value))
            {
                deadPositions[value] = ImportIndex;
            }
            return true;
        }

        /// <summary>
        /// Adds a particular value to this block's list of exports.
        /// If <paramref name="value"/> is not defined by this block,
        /// then it is imported.
        /// </summary>
        /// <param name="value">
        /// The value to import.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value is newly exported; otherwise, <c>false</c>.
        /// </returns>
        internal bool Export(ValueTag value)
        {
            return UseAt(value, ExportIndex);
        }

        /// <summary>
        /// Hints that a particular value is used at a
        /// particular index.
        /// </summary>
        /// <param name="value">
        /// The tag of the value that is used.
        /// </param>
        /// <param name="index">
        /// The index at which the tag is used.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value is newly used; otherwise, <c>false</c>.
        /// </returns>
        private bool UseAt(ValueTag value, int index)
        {
            int previousIndex;
            if (deadPositions.TryGetValue(value, out previousIndex)
                && previousIndex >= index)
            {
                return false;
            }

            deadPositions[value] = index;
            if (!IsDefinedOrImported(value))
            {
                Import(value);
            }
            return true;
        }

        /// <summary>
        /// Creates block liveness data for a particular block.
        /// Live and dead positions are recorded for all definitions
        /// and uses in the block.
        /// </summary>
        /// <param name="block">The block to analyze.</param>
        /// <returns>
        /// Block liveness data for a single block.
        /// </returns>
        internal static BlockLiveness Create(BasicBlock block)
        {
            var result = new BlockLiveness(block);

            // Mark parameters as defined here.
            foreach (var parameter in block.Parameters)
            {
                result.livePositions.Add(parameter.Tag, ParameterIndex);
                result.deadPositions.Add(parameter.Tag, ParameterIndex);
            }

            // Mark instructions as defined here, use their arguments.
            foreach (var selection in block.NamedInstructions)
            {
                result.livePositions.Add(selection.Tag, selection.InstructionIndex);
                result.deadPositions.Add(selection.Tag, selection.InstructionIndex);

                foreach (var argTag in selection.Instruction.Arguments)
                {
                    result.UseAt(argTag, selection.InstructionIndex);
                }
            }

            int insnCount = block.InstructionTags.Count;
            foreach (var flowInsn in block.Flow.Instructions)
            {
                foreach (var argTag in flowInsn.Arguments)
                {
                    result.UseAt(argTag, insnCount);
                }
                insnCount++;
            }

            foreach (var branch in block.Flow.Branches)
            {
                foreach (var arg in branch.Arguments)
                {
                    if (arg.IsValue)
                    {
                        result.Export(arg.ValueOrNull);
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Describes which variables are live at any location
    /// in a control flow graph.
    /// </summary>
    public struct ValueLiveness
    {
        internal ValueLiveness(
            Dictionary<BasicBlockTag, BlockLiveness> liveness)
        {
            this.liveness = liveness;
        }

        private Dictionary<BasicBlockTag, BlockLiveness> liveness;

        /// <summary>
        /// Gets block liveness data for a particular block.
        /// </summary>
        /// <param name="tag">
        /// The tag of the block to get the liveness data for.
        /// </param>
        /// <returns>
        /// Block liveness data.
        /// </returns>
        public BlockLiveness GetLiveness(BasicBlockTag tag)
        {
            return liveness[tag];
        }
    }

    /// <summary>
    /// An analysis that determines which variables are live at any location
    /// in a control flow graph.
    /// </summary>
    public sealed class LivenessAnalysis : IFlowGraphAnalysis<ValueLiveness>
    {
        private LivenessAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the liveness analysis.
        /// </summary>
        /// <returns>An instance of the liveness analysis.</returns>
        public static readonly LivenessAnalysis Instance = new LivenessAnalysis();

        /// <inheritdoc/>
        public ValueLiveness Analyze(FlowGraph graph)
        {
            var liveness = new Dictionary<BasicBlockTag, BlockLiveness>();
            // First create liveness data for blocks in isolation.
            foreach (var block in graph.BasicBlocks)
            {
                liveness[block.Tag] = BlockLiveness.Create(block);
            }

            var preds = graph.GetAnalysisResult<BasicBlockPredecessors>();

            // Then propagate imports until a fixpoint is reached.
            // The idea is to create a worklist of basic blocks that
            // still need to be processed.
            var worklist = new Queue<BasicBlockTag>(graph.BasicBlockTags);
            var workset = new HashSet<BasicBlockTag>(graph.BasicBlockTags);

            while (worklist.Count > 0)
            {
                // Dequeue a block from the worklist.
                var blockTag = worklist.Dequeue();
                workset.Remove(blockTag);

                var blockData = liveness[blockTag];

                // Propagate imports to predecessors.
                foreach (var predTag in preds.GetPredecessorsOf(blockTag))
                {
                    // Every predecessor must export each and every one of the
                    // block's imports.
                    var predData = liveness[predTag];
                    foreach (var import in blockData.Imports)
                    {
                        // Have the predecessor export the imported value.
                        if (predData.Export(import))
                        {
                            // The predecessor block doesn't define the imported value
                            // and hence also had to import the imported value. Add the
                            // predecessor block to the worklist.
                            if (workset.Add(predTag))
                            {
                                worklist.Enqueue(predTag);
                            }
                        }
                    }
                }
            }

            return new ValueLiveness(liveness);
        }

        /// <inheritdoc/>
        public ValueLiveness AnalyzeWithUpdates(
            FlowGraph graph,
            ValueLiveness previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
