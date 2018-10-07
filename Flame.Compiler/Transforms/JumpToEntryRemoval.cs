using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Flow;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transform that removes all direct jumps to the entry point by
    /// replacing the current entry point with a 'thunk' basic block that
    /// is entered once per function call.
    /// </summary>
    public sealed class JumpToEntryRemoval : IntraproceduralOptimization
    {
        private JumpToEntryRemoval()
        { }

        /// <summary>
        /// An instance of the jump-to-entry removal transform.
        /// </summary>
        public static readonly JumpToEntryRemoval Instance = new JumpToEntryRemoval();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            BasicBlockTag entryThunk = null;
            foreach (var block in builder.BasicBlocks)
            {
                var flow = block.Flow;
                foreach (var branch in block.Flow.Branches)
                {
                    if (branch.Target == graph.EntryPointTag)
                    {
                        if (entryThunk == null)
                        {
                            entryThunk = CreateEntryPointThunk(builder);
                            break;
                        }
                    }
                }
                if (entryThunk != null)
                {
                    break;
                }
            }
            if (entryThunk != null)
            {
                builder.EntryPointTag = entryThunk;
            }
            return builder.ToImmutable();
        }

        private static BasicBlockTag CreateEntryPointThunk(FlowGraphBuilder builder)
        {
            var thunk = builder.AddBasicBlock(builder.EntryPointTag.Name + ".thunk");

            foreach (var param in builder.GetBasicBlock(builder.EntryPointTag).Parameters)
            {
                thunk.AppendParameter(new BlockParameter(param.Type, param.Tag.Name + ".thunk"));
            }

            thunk.Flow = new JumpFlow(
                builder.EntryPointTag,
                thunk.ParameterTags.ToArray());

            return thunk;
        }
    }
}
