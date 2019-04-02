namespace Flame.Compiler
{
    /// <summary>
    /// A reference to an instruction in a control-flow graph, which
    /// can either be a named instruction that is defined directly by
    /// a basic block or an unnamed instruction included in a block's
    /// flow.
    ///
    /// This is a mutable reference: it refers to an instruction in
    /// a control-flow graph builder and that instruction can be changed.
    /// </summary>
    public abstract class MutableInstructionRef
    {
        /// <summary>
        /// Gets or sets the instruction referred to by this
        /// instruction reference.
        /// </summary>
        /// <value>
        /// The instruction referred to by this reference.
        /// </value>
        public abstract Instruction Instruction { get; set; }

        /// <summary>
        /// Replaces the instruction referred to by this instruction
        /// reference with a control-flow graph that implements the
        /// instruction.
        /// </summary>
        /// <param name="implementation">
        /// A control-flow graph that implements the instruction.
        /// </param>
        public abstract void ReplaceInstruction(FlowGraph graph);
    }
}
