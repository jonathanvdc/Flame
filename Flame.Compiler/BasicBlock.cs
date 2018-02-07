using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flame.Compiler
{
    /// <summary>
    /// A basic block in a control-flow graph.
    /// </summary>
    public struct BasicBlock
    {
        internal BasicBlock(FlowGraph graph, BasicBlockTag tag, BasicBlockData data)
        {
            this = default(BasicBlock);
            this.Graph = graph;
            this.Tag = tag;
            this.data = data;
        }

        /// <summary>
        /// Gets the control-flow graph in which this block resides.
        /// </summary>
        /// <returns>A control-flow graph.</returns>
        public FlowGraph Graph { get; private set; }

        /// <summary>
        /// Gets this basic block's tag.
        /// </summary>
        /// <returns>The basic block's tag.</returns>
        public BasicBlockTag Tag { get; private set; }

        private BasicBlockData data;

        /// <summary>
        /// Gets this basic block's list of parameters.
        /// </summary>
        /// <returns>The basic block's parameters.</returns>
        public ImmutableList<BlockParameter> Parameters => data.Parameters;

        /// <summary>
        /// Gets the list of all instruction tags in this basic block.
        /// </summary>
        /// <returns>The list of all instruction tags.</returns>
        public ImmutableList<ValueTag> InstructionTags => data.InstructionTags;

        /// <summary>
        /// Gets the control flow at the end of this basic block.
        /// </summary>
        /// <returns>The end-of-block control flow.</returns>
        public BlockFlow Flow => data.Flow;

        /// <summary>
        /// Creates a new basic block in a new control-flow graph that
        /// has a particular flow.
        /// </summary>
        /// <param name="flow">The new flow.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock WithFlow(BlockFlow flow)
        {
            return Graph.UpdateBasicBlockFlow(Tag, flow);
        }

        /// <summary>
        /// Creates a new basic block in a new control-flow graph that
        /// has a particular list of parameters.
        /// </summary>
        /// <param name="parameters">The new parameters.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock WithParameters(IReadOnlyList<BlockParameter> parameters)
        {
            return WithParameters(parameters.ToImmutableList<BlockParameter>());
        }

        /// <summary>
        /// Creates a new basic block in a new control-flow graph that
        /// has a particular list of parameters.
        /// </summary>
        /// <param name="parameters">The new parameters.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock WithParameters(ImmutableList<BlockParameter> parameters)
        {
            return Graph.UpdateBasicBlockParameters(Tag, parameters);
        }
    }
}