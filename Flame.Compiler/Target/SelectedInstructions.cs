using System.Collections.Generic;

namespace Flame.Compiler.Target
{
    /// <summary>
    /// A collection of selected instructions for a value.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of instructions.
    /// </typeparam>
    public struct SelectedInstructions<TInstruction>
    {
        /// <summary>
        /// Creates a selected instruction container.
        /// </summary>
        /// <param name="instructions">
        /// The instructions selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public SelectedInstructions(
            IReadOnlyList<TInstruction> instructions,
            IReadOnlyList<ValueTag> dependencies)
        {
            this.Instructions = instructions;
            this.Dependencies = dependencies;
        }

        /// <summary>
        /// Creates a selected instruction container from a single
        /// instruction and a variable number of dependencies.
        /// </summary>
        /// <param name="instruction">
        /// The instruction selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public SelectedInstructions(
            TInstruction instruction,
            params ValueTag[] dependencies)
            : this(new[] { instruction }, dependencies)
        { }

        /// <summary>
        /// Gets the list of instructions in this container.
        /// </summary>
        /// <value>A list of instructions.</value>
        public IReadOnlyList<TInstruction> Instructions { get; private set; }

        /// <summary>
        /// Gets the list of values these selected
        /// instructions depend on.
        /// </summary>
        /// <value>A list of values.</value>
        public IReadOnlyList<ValueTag> Dependencies { get; private set; }
    }

    /// <summary>
    /// A collection of selected instructions for a value.
    /// </summary>
    public static class SelectedInstructions
    {
        /// <summary>
        /// Creates a selected instruction container.
        /// </summary>
        /// <param name="instructions">
        /// The instructions selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public static SelectedInstructions<TInstruction> Create<TInstruction>(
            IReadOnlyList<TInstruction> instructions,
            IReadOnlyList<ValueTag> dependencies)
        {
            return new SelectedInstructions<TInstruction>(instructions, dependencies);
        }

        /// <summary>
        /// Creates a selected instruction container from a single
        /// instruction and a variable number of dependencies.
        /// </summary>
        /// <param name="instruction">
        /// The instruction selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public static SelectedInstructions<TInstruction> Create<TInstruction>(
            TInstruction instruction,
            params ValueTag[] dependencies)
        {
            return new SelectedInstructions<TInstruction>(instruction, dependencies);
        }
    }
}
