namespace Flame.Compiler
{
    /// <summary>
    /// An instruction in a mutable control-flow graph builder.
    /// </summary>
    public sealed class InstructionBuilder
    {
        /// <summary>
        /// Creates an instruction builder from a graph and a tag.
        /// </summary>
        /// <param name="graph">The instruction builder's defining graph.</param>
        /// <param name="tag">The instruction's tag.</param>
        internal InstructionBuilder(FlowGraphBuilder graph, ValueTag tag)
        {
            this.Graph = graph;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets this instruction's tag.
        /// </summary>
        /// <returns>The instruction's tag.</returns>
        public ValueTag Tag { get; private set; }

        /// <summary>
        /// Gets the control-flow graph builder that defines this
        /// instruction.
        /// </summary>
        /// <returns>The control-flow graph builder.</returns>
        public FlowGraphBuilder Graph { get; private set; }

        /// <summary>
        /// Tells if this instruction builder is still valid, that is,
        /// it has not been removed from its control-flow graph builder's
        /// set of instructions.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instruction builder is still valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid => Graph.ContainsInstruction(Tag);
    }
}