using System.Collections.Generic;
using Flame.Collections;
using Flame.Compiler.Flow;
using System.Collections.Immutable;

namespace Flame.Compiler
{
    /// <summary>
    /// An immutable control-flow graph that consists of basic blocks.
    /// </summary>
    public sealed class FlowGraph
    {
        /// <summary>
        /// Creates a control-flow graph that contains only an empty
        /// entry point block.
        /// </summary>
        public FlowGraph()
        {
            this.instructions = ImmutableDictionary.Create<ValueTag, Instruction>();
            this.blocks = ImmutableDictionary.Create<BasicBlockTag, BasicBlockData>();
            // TODO: create entry point block
            throw new System.NotImplementedException();
        }

        private FlowGraph(
            FlowGraph other)
        {
            this.instructions = other.instructions;
            this.blocks = other.blocks;
            this.EntryPointTag = other.EntryPointTag;
        }

        private ImmutableDictionary<ValueTag, Instruction> instructions;
        private ImmutableDictionary<BasicBlockTag, BasicBlockData> blocks;

        /// <summary>
        /// Gets the tag of the entry point block.
        /// </summary>
        /// <returns>The tag of the entry point block.</returns>
        public BasicBlockTag EntryPointTag { get; private set; }

        public BasicBlock AddBasicBlock(string name)
        {
            var tag = new BasicBlockTag(name);
            var data = new BasicBlockData(
                EmptyArray<BlockParameter>.Value,
                EmptyArray<ValueTag>.Value,
                UnreachableFlow.Instance);
            var newGraph = new FlowGraph(this);
            newGraph.blocks = newGraph.blocks.Add(tag, data);
            return new BasicBlock(newGraph, tag, data);
        }

        /// <summary>
        /// Gets the basic block with a particular tag.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        /// <returns>A basic block.</returns>
        public BasicBlock GetBasicBlock(BasicBlockTag tag)
        {
            return new BasicBlock(this, tag, blocks[tag]);
        }

        internal BasicBlock UpdateBasicBlockData(BasicBlockTag tag, BasicBlockData data)
        {
            var newGraph = new FlowGraph(this);
            newGraph.blocks = newGraph.blocks.SetItem(tag, data);
            return new BasicBlock(newGraph, tag, data);
        }
    }
}