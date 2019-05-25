using System.Collections.Generic;

namespace Flame.Compiler.Target
{
    /// <summary>
    /// A base class for instruction selection algorithms.
    /// Instruction selectors take IR instructions and turn them into
    /// sequences of target-specific instructions.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of instructions to produce.
    /// </typeparam>
    public interface IInstructionSelector<TInstruction>
    {
        /// <summary>
        /// Selects instructions for a particular IR instruction.
        /// </summary>
        /// <param name="instruction">
        /// The IR instruction to translate to target-specific instructions.
        /// </param>
        /// <returns>
        /// A batch of selected instructions.
        /// </returns>
        SelectedInstructions<TInstruction> SelectInstructions(
            NamedInstruction instruction);
    }

    /// <summary>
    /// An instruction selection algorithm for instruction sets that produce
    /// linear streams of instructions, that is, control flow is expressed
    /// using branches to branch targets.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of instructions to produce.
    /// </typeparam>
    public interface ILinearInstructionSelector<TInstruction> : IInstructionSelector<TInstruction>
    {
        /// <summary>
        /// Selects instructions for a particular IR block flow.
        /// </summary>
        /// <param name="flow">
        /// The IR block flow to translate to target-specific instructions.
        /// </param>
        /// <param name="blockTag">
        /// The tag of the basic block that defines the flow.
        /// </param>
        /// <param name="graph">
        /// The IR graph that defines the flow.
        /// </param>
        /// <param name="preferredFallthrough">
        /// A preferred fallthrough block, which will likely result in better
        /// codegen if chosen as fallthrough. May be <c>null</c>.
        /// </param>
        /// <param name="fallthrough">
        /// The fallthrough block expected by the selected instruction,
        /// if any.
        /// </param>
        /// <returns>
        /// A batch of selected instructions.
        /// </returns>
        SelectedFlowInstructions<TInstruction> SelectInstructions(
            BlockFlow flow,
            BasicBlockTag blockTag,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough);

        /// <summary>
        /// Creates a sequence of instructions that declare the start of a basic block.
        /// </summary>
        /// <param name="block">
        /// The basic block to mark.
        /// </param>
        /// <returns>
        /// A sequence of instructions that mark a block.
        /// </returns>
        IReadOnlyList<TInstruction> CreateBlockMarker(BasicBlock block);

        /// <summary>
        /// Creates an unconditional jump to a particular branch target.
        /// </summary>
        /// <param name="target">
        /// A basic block tag that uniquely identifies a branch target.
        /// </param>
        /// <returns>
        /// An unconditional jump.
        /// </returns>
        IReadOnlyList<TInstruction> CreateJumpTo(BasicBlockTag target);
    }

    /// <summary>
    /// An instruction selector for a stack machine that can transfer data using either
    /// stack slots or register loads and stores.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of instructions to produce.
    /// </typeparam>
    public interface IStackInstructionSelector<TInstruction> : IInstructionSelector<TInstruction>
    {
        /// <summary>
        /// Tells if instances of a particular instruction prototype
        /// actually push a value onto the stack. Instructions that do
        /// not push values must either ensure that their result is
        /// never used or spill their result into the appropriate virtual
        /// register on their own.
        /// </summary>
        /// <param name="prototype">The instruction prototype to inspect.</param>
        /// <returns>
        /// <c>true</c> if instances of <paramref name="prototype"/> push a value
        /// onto the stack; otherwise <c>false</c>.
        /// </returns>
        bool Pushes(InstructionPrototype prototype);

        /// <summary>
        /// Creates instructions that pop a value from the stack, discarding it.
        /// </summary>
        /// <param name="type">The type of value to pop.</param>
        /// <returns>A list of selected instructions.</returns>
        IReadOnlyList<TInstruction> CreatePop(IType type);

        /// <summary>
        /// Tries to create instructions that duplicate the top-of-stack value.
        /// </summary>
        /// <param name="type">The type of value to duplicate.</param>
        /// <param name="dup">
        /// A sequence of instructions that duplicate the top-of-stack value, if
        /// they can be created.
        /// </param>
        /// <returns>
        /// <c>true</c> if a sequence of instructions exists that can efficiently
        /// duplicate the top-of-stack value; otherwise, <c>false</c>.
        /// </returns>
        bool TryCreateDup(IType type, out IReadOnlyList<TInstruction> dup);

        /// <summary>
        /// Creates instructions that load a value from its virtual register.
        /// </summary>
        /// <param name="value">The value to load.</param>
        /// <param name="type">The type of <paramref name="value"/>.</param>
        /// <returns>A list of selected instructions.</returns>
        IReadOnlyList<TInstruction> CreateLoadRegister(ValueTag value, IType type);

        /// <summary>
        /// Creates instructions that stores a value into its virtual register.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <param name="type">The type of <paramref name="value"/>.</param>
        /// <returns>A list of selected instructions.</returns>
        IReadOnlyList<TInstruction> CreateStoreRegister(ValueTag value, IType type);
    }
}
