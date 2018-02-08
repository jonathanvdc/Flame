using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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
        /// Checks if a particular instance of this prototype conforms to
        /// the rules for this instruction prototype.
        /// </summary>
        /// <param name="instance">
        /// An instance of this prototype.
        /// </param>
        /// <param name="body">
        /// The method body that defines the instruction.
        /// </param>
        /// <returns>
        /// A list of conformance errors in the instruction.
        /// </returns>
        public abstract IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body);

        /// <summary>
        /// Instantiates this prototype with a list of arguments.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to instantiate this prototype with.
        /// </param>
        /// <returns>
        /// An instruction whose prototype is equal to this prototype
        /// and whose argument list is <paramref name="arguments"/>.
        /// </returns>
        public Instruction Instantiate(IReadOnlyList<ValueTag> arguments)
        {
            ContractHelpers.Assert(arguments.Count == ParameterCount);
            return new Instruction(this, arguments);
        }
    }
}