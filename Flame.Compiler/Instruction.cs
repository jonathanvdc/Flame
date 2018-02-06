using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// A base class for instructions: statements that produce
    /// a single value.
    /// </summary>
    public abstract class Instruction
    {
        /// <summary>
        /// Gets the type of value produced by the instruction.
        /// </summary>
        /// <returns>The type of value.</returns>
        public abstract IType Type { get; }

        /// <summary>
        /// Gets the list of values this instruction takes as arguments.
        /// </summary>
        /// <returns>The values taken as arguments.</returns>
        public abstract IReadOnlyList<ValueTag> Arguments { get; }

        /// <summary>
        /// Replaces this instruction's arguments with a particular
        /// list of arguments.
        /// </summary>
        /// <param name="arguments">The new arguments.</param>
        /// <returns>A new instruction.</returns>
        public abstract Instruction WithArguments(IReadOnlyList<ValueTag> arguments);
    }
}