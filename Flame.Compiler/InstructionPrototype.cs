using System;
using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// Describes an instruction's prototype: everything there is to
    /// an instruction except for its arguments.
    /// </summary>
    public abstract class InstructionPrototype
    {
        /// <summary>
        /// Gets the type of value produced instantiations of this prototype.
        /// </summary>
        /// <returns>A type of value.</returns>
        public abstract IType ResultType { get; }

        /// <summary>
        /// Gets the number of arguments this instruction takes when instantiated.
        /// </summary>
        /// <returns>The number of arguments this instruction takes.</returns>
        public abstract int ParameterCount { get; }

        /// <summary>
        /// Instantiates this prototype with a list of arguments.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to instantiate this prototype with.
        /// </param>
        /// <returns>
        /// An instruction whose prototype is equal to this prototype
        /// and whose argument list corresponds to <paramref name="arguments"/>.
        /// </returns>
        public Instruction Instantiate(IReadOnlyList<ValueTag> arguments)
        {
            ContractHelpers.Assert(arguments.Count == ParameterCount);
            return new Instruction(this, arguments);
        }
    }
}