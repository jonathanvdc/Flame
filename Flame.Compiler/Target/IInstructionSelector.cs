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
            SelectedInstruction instruction);
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
        SelectedInstructions<TInstruction> SelectInstructions(
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
}
