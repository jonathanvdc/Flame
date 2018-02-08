using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// An instructions: a statement that produces a single value.
    /// </summary>
    public abstract class Instruction
    {
        /// <summary>
        /// Gets a list of values this instruction takes as arguments.
        /// </summary>
        /// <returns>The values taken as arguments.</returns>
        public abstract IReadOnlyList<ValueTag> Arguments { get; }

        /// <summary>
        /// Gets this instruction's prototype.
        /// </summary>
        /// <returns>The prototype.</returns>
        public abstract InstructionPrototype Prototype { get; }

        /// <summary>
        /// Gets the type of value produced by this instruction.
        /// </summary>
        /// <returns>A type of value.</returns>
        public IType ResultType => Prototype.ResultType;
    }
}