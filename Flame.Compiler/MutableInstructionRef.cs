using System.Collections.Generic;

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
        /// Tells if this instruction reference is still valid. Querying
        /// or modifying invalid instruction references results in an
        /// exception.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instruction reference is valid; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsValid { get; }

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
        /// <param name="arguments">
        /// A list of arguments to pass to <paramref name="implementation"/>'s
        /// entry point block.
        /// </param>
        /// <remarks>
        /// Calling this method may invalidate instruction references,
        /// including this reference. Specifically, if this reference
        /// refers to an unnamed instruction in block flow, then this
        /// reference and all other references to unnamed instructions
        /// in that block flow may be invalidated.
        /// </remarks>
        public abstract void ReplaceInstruction(
            FlowGraph implementation,
            IReadOnlyList<ValueTag> arguments);

        /// <summary>
        /// Replaces the instruction referred to by this instruction
        /// reference with a control-flow graph that implements the
        /// instruction. The instruction's arguments are passed to
        /// <paramref name="implementation"/>'s entry point block.
        /// </summary>
        /// <param name="implementation">
        /// A control-flow graph that implements the instruction.
        /// </param>
        /// <remarks>
        /// Calling this method may invalidate instruction references,
        /// including this reference. Specifically, if this reference
        /// refers to an unnamed instruction in block flow, then this
        /// reference and all other references to unnamed instructions
        /// in that block flow may be invalidated.
        /// </remarks>
        public void ReplaceInstruction(FlowGraph implementation)
        {
            ReplaceInstruction(implementation, Instruction.Arguments);
        }

        /// <summary>
        /// Gets the instruction's result type.
        /// </summary>
        public IType ResultType => Instruction.ResultType;

        /// <summary>
        /// Gets the instruction's prototype.
        /// </summary>
        public InstructionPrototype Prototype => Instruction.Prototype;
    }
}
