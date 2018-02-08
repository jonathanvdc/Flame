using System;
using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// Describes an instruction's prototype: everything there is to
    /// an instruction except for its arguments.
    /// </summary>
    public abstract class InstructionPrototype : IEquatable<InstructionPrototype>
    {
        /// <summary>
        /// Gets the type of value produced instantiations of this prototype.
        /// </summary>
        /// <returns>A type of value.</returns>
        public abstract IType ResultType { get; }

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
        public abstract Instruction Instantiate(IReadOnlyList<ValueTag> arguments);

        /// <summary>
        /// Tests if this instruction prototype equals to another
        /// instruction prototype.
        /// </summary>
        /// <param name="other">The other instruction prototype.</param>
        /// <returns>
        /// <c>true</c> if this prototype equals the other prototype;
        /// otherwise, <c>false</c>.
        /// </returns>
        public abstract bool Equals(InstructionPrototype other);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is InstructionPrototype
                && Equals((InstructionPrototype)obj);
        }

        /// <inheritdoc/>
        public abstract override int GetHashCode();
    }
}