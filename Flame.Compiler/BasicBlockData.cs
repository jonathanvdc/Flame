using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// The data behind a basic block in a control-flow graph.
    /// </summary>
    internal sealed class BasicBlockData
    {
        /// <summary>
        /// Creates a basic block from a list of parameters,
        /// a list of instructions and an end-of-block control
        /// flow instruction.
        /// </summary>
        /// <param name="parameters">
        /// The list of parameters for the basic block.
        /// </param>
        /// <param name="instructions">
        /// The list of instructions for the basic block.
        /// </param>
        /// <param name="flow">
        /// The block's end-of-block control flow.
        /// </param>
        public BasicBlockData(
            IReadOnlyList<BlockParameter> parameters,
            IReadOnlyList<ValueTag> instructions,
            BlockFlow flow)
        {
            this.Parameters = parameters;
            this.Instructions = instructions;
            this.Flow = flow;
        }

        /// <summary>
        /// Gets this basic block's list of parameters.
        /// </summary>
        /// <returns>The basic block's parameters.</returns>
        public IReadOnlyList<BlockParameter> Parameters { get; private set; }

        /// <summary>
        /// Gets the list of instructions in this basic block.
        /// </summary>
        /// <returns>The list of instructions.</returns>
        public IReadOnlyList<ValueTag> Instructions { get; private set; }

        /// <summary>
        /// Gets the control flow at the end of this basic block.
        /// </summary>
        /// <returns>The end-of-block control flow.</returns>
        public BlockFlow Flow { get; private set; }
    }
}