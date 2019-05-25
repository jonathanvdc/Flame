using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that fuses basic blocks, eliminating
    /// unnecessary jumps.
    /// </summary>
    public sealed class BlockFusion : IntraproceduralOptimization
    {
        private BlockFusion()
        { }

        /// <summary>
        /// An instance of the block fusion transform.
        /// </summary>
        public static readonly BlockFusion Instance = new BlockFusion();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // Build a mapping of blocks to their predecessors.
            var predecessors = graph.GetAnalysisResult<BasicBlockPredecessors>();

            // Figure out which blocks jump unconditionally to a block
            // with only one predecessor.
            var fusible = new HashSet<BasicBlockTag>();
            foreach (var block in graph.BasicBlockTags)
            {
                BasicBlockTag tail;
                if (TryGetFusibleTail(block, graph, predecessors, out tail))
                {
                    fusible.Add(block);
                }
            }

            // Start editing the graph.
            var builder = graph.ToBuilder();

            // Maintain a dictionary of values that need to be
            // replaced with other values.
            var replacements = new Dictionary<ValueTag, ValueTag>();

            // Maintain a set of blocks to delete.
            var deadBlocks = new HashSet<BasicBlockTag>();

            // Fuse fusible blocks.
            while (fusible.Count > 0)
            {
                // Pop an item from the worklist.
                var tag = fusible.First();

                // Grab the block to edit.
                var block = builder.GetBasicBlock(tag);

                // Grab the successor block to which the block jumps.
                var branch = ((JumpFlow)block.Flow).Branch;
                var successor = builder.GetBasicBlock(branch.Target);

                // Update the worklist.
                if (!fusible.Remove(successor))
                {
                    fusible.Remove(tag);
                }

                // Replace branch parameters.
                foreach (var pair in branch.ZipArgumentsWithParameters(builder))
                {
                    replacements.Add(pair.Key, pair.Value.ValueOrNull);
                }

                // Move instructions around.
                foreach (var instruction in successor.NamedInstructions)
                {
                    instruction.MoveTo(block);
                }

                // Update the block's flow.
                block.Flow = successor.Flow;
            }

            // Replace instruction uses.
            builder.ReplaceUses(replacements);

            // Delete dead blocks.
            foreach (var tag in deadBlocks)
            {
                builder.RemoveBasicBlock(tag);
            }

            return builder.ToImmutable();
        }

        internal static bool TryGetFusibleTail(
            BasicBlockTag head,
            FlowGraph graph,
            BasicBlockPredecessors predecessors,
            out BasicBlockTag tail)
        {
            var block = graph.GetBasicBlock(head);
            var jumpFlow = block.Flow as JumpFlow;
            if (jumpFlow != null && graph.EntryPointTag != jumpFlow.Branch.Target)
            {
                var preds = predecessors.GetPredecessorsOf(jumpFlow.Branch.Target).ToArray();
                if (preds.Length == 1 && preds[0] == block.Tag)
                {
                    tail = jumpFlow.Branch.Target;
                    return true;
                }
            }
            tail = null;
            return false;
        }
    }
}
