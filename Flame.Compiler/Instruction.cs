using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler
{
    /// <summary>
    /// An instruction: a statement that produces a single value.
    /// </summary>
    public partial struct Instruction : IEquatable<Instruction>
    {
        internal Instruction(
            InstructionPrototype prototype,
            IReadOnlyList<ValueTag> arguments)
        {
            this.Prototype = prototype;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets this instruction's prototype.
        /// </summary>
        /// <returns>The prototype.</returns>
        public InstructionPrototype Prototype { get; private set; }

        /// <summary>
        /// Gets a list of values this instruction takes as arguments.
        /// </summary>
        /// <returns>The values taken as arguments.</returns>
        public IReadOnlyList<ValueTag> Arguments { get; private set; }

        /// <summary>
        /// Gets the type of value produced by this instruction.
        /// </summary>
        /// <returns>A type of value.</returns>
        public IType ResultType => Prototype.ResultType;

        /// <summary>
        /// Checks if this instruction conforms to the rules for its
        /// instruction prototype.
        /// </summary>
        /// <param name="body">
        /// The method body that defines this instruction.
        /// </param>
        /// <returns>
        /// A list of validation errors.
        /// </returns>
        public IReadOnlyList<string> Validate(MethodBody body)
        {
            return Prototype.CheckConformance(this, body);
        }

        /// <summary>
        /// Applies a member mapping to this instruction.
        /// </summary>
        /// <param name="mapping">A member mapping.</param>
        /// <returns>A transformed prototype.</returns>
        public Instruction Map(MemberMapping mapping)
        {
            return Prototype.Map(mapping).Instantiate(Arguments);
        }

        /// <summary>
        /// Tests if this instruction is (superficially) identical to
        /// another instruction.
        /// </summary>
        /// <param name="other">The other instruction.</param>
        /// <returns>
        /// <c>true</c> if this instruction has the same prototype and
        /// arguments as <paramref name="other"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Instruction other)
        {
            return Prototype.Equals(other.Prototype)
                && Arguments.SequenceEqual<ValueTag>(other.Arguments);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Instruction && Equals((Instruction)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = EnumerableComparer.EmptyHash;
            int argCount = Arguments.Count;
            for (int i = 0; i < argCount; i++)
            {
                hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, Arguments[i]);
            }
            hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, Prototype);
            return hashCode;
        }

        /// <summary>
        /// Tests if the first instruction equals the second.
        /// </summary>
        /// <param name="left">The first instruction.</param>
        /// <param name="right">The second instruction.</param>
        /// <returns>
        /// <c>true</c> if the instructions have identical prototypes
        /// and arguments; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator==(Instruction left, Instruction right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests if the first instruction does not equal the second.
        /// </summary>
        /// <param name="left">The first instruction.</param>
        /// <param name="right">The second instruction.</param>
        /// <returns>
        /// <c>false</c> if the instructions have identical prototypes
        /// and arguments; otherwise, <c>true</c>.
        /// </returns>
        public static bool operator!=(Instruction left, Instruction right)
        {
            return !left.Equals(right);
        }
    }
}
