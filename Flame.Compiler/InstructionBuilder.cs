using System.Collections.Generic;
using System.Linq;

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
    public abstract class InstructionBuilder
    {
        /// <summary>
        /// Tells if this instruction builder is still valid. Querying
        /// or modifying invalid instruction builders results in an
        /// exception.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instruction reference is valid; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsValid { get; }

        /// <summary>
        /// Gets the block that defines this instruction.
        /// </summary>
        /// <value>A basic block builder.</value>
        public abstract BasicBlockBuilder Block { get; }

        /// <summary>
        /// Gets the graph that defines this instruction.
        /// </summary>
        /// <value>A control-flow graph builder.</value>
        public virtual FlowGraphBuilder Graph => Block.Graph;

        /// <summary>
        /// Gets or sets the instruction referred to by this
        /// instruction builder.
        /// </summary>
        /// <value>
        /// The instruction referred to by this builder.
        /// </value>
        public abstract Instruction Instruction { get; set; }

        /// <summary>
        /// Replaces the instruction referred to by this instruction
        /// builder with a control-flow graph that implements the
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
        /// Calling this method may invalidate instruction builders,
        /// including this builder. Specifically, if this builder
        /// refers to an unnamed instruction in block flow, then this
        /// builder and all other builders to unnamed instructions
        /// in that block flow may be invalidated.
        /// </remarks>
        public abstract void ReplaceInstruction(
            FlowGraph implementation,
            IReadOnlyList<ValueTag> arguments);

        /// <summary>
        /// Replaces the instruction referred to by this instruction
        /// builder with a control-flow graph that implements the
        /// instruction. The instruction's arguments are passed to
        /// <paramref name="implementation"/>'s entry point block.
        /// </summary>
        /// <param name="implementation">
        /// A control-flow graph that implements the instruction.
        /// </param>
        /// <remarks>
        /// Calling this method may invalidate instruction builders,
        /// including this builder. Specifically, if this builder
        /// refers to an unnamed instruction in block flow, then this
        /// builder and all other builders to unnamed instructions
        /// in that block flow may be invalidated.
        /// </remarks>
        public void ReplaceInstruction(FlowGraph implementation)
        {
            ReplaceInstruction(implementation, Instruction.Arguments);
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The tag to assign to the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public abstract NamedInstructionBuilder InsertBefore(Instruction instruction, ValueTag tag);

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstructionBuilder InsertBefore(Instruction instruction, string name)
        {
            return InsertBefore(instruction, new ValueTag(name));
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstructionBuilder InsertBefore(Instruction instruction)
        {
            return InsertBefore(instruction, "");
        }

        /// <summary>
        /// Gets the instruction's result type.
        /// </summary>
        public IType ResultType => Instruction.ResultType;

        /// <summary>
        /// Gets the instruction's prototype.
        /// </summary>
        public InstructionPrototype Prototype => Instruction.Prototype;

        /// <summary>
        /// Gets or sets the instruction's argument list.
        /// </summary>
        public IReadOnlyList<ValueTag> Arguments
        {
            get
            {
                return Instruction.Arguments;
            }
            set
            {
                if (!Instruction.Arguments.SequenceEqual(value))
                {
                    // Only update the instruction if the new arguments
                    // differ from the old ones. CFG updates invalidate
                    // analyses, so we want to suppress spurious updates
                    // as much as possible.
                    Instruction = Instruction.WithArguments(value);
                }
            }
        }
    }
}
