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
        /// Creates a selection instruction container.
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
}
