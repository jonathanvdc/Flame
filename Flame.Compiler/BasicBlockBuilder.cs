namespace Flame.Compiler
{
    /// <summary>
    /// A basic block in a mutable control-flow graph builder.
    /// </summary>
    public sealed class BasicBlockBuilder
    {
        /// <summary>
        /// Creates a basic block builder from a graph and a tag.
        /// </summary>
        /// <param name="graph">The basic block builder's defining graph.</param>
        /// <param name="tag">The basic block's tag.</param>
        internal BasicBlockBuilder(FlowGraphBuilder graph, BasicBlockTag tag)
        {
            this.Graph = graph;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets this basic block's tag.
        /// </summary>
        /// <returns>The basic block's tag.</returns>
        public BasicBlockTag Tag { get; private set; }

        /// <summary>
        /// Gets the control-flow graph builder that defines this
        /// basic block.
        /// </summary>
        /// <returns>The control-flow graph builder.</returns>
        public FlowGraphBuilder Graph { get; private set; }

        /// <summary>
        /// Tells if this basic block builder is still valid, that is,
        /// it has not been removed from its control-flow graph builder's
        /// set of basic blocks.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this basic block builder is still valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid => Graph.ContainsBasicBlock(Tag);
    }
}