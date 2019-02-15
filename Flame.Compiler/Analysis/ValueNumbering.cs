using System.Linq;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A mapping of values to representative values for a particular
    /// control-flow graph.
    /// </summary>
    public abstract class ValueNumbering
    {
        /// <summary>
        /// Gets a value's "number," i.e., another value that
        /// is representative of the set of all values that are
        /// equivalent with the value.
        /// </summary>
        /// <param name="value">A value tag to examine.</param>
        /// <returns>
        /// The set representative for the set of all values equivalent
        /// with <paramref name="value"/>. Requesting the set
        /// representative of another value that is equivalent with
        /// <paramref name="value"/> will produce the same set
        /// representative.
        /// </returns>
        public abstract ValueTag GetNumber(ValueTag value);

        /// <summary>
        /// Tests if two values are equivalent. Values 'a', 'b' are considered
        /// to the equivalent iff 'a' dominates 'b' implies that 'b' can be
        /// replaced with a copy of 'a'.
        /// </summary>
        /// <param name="first">The first value to consider.</param>
        /// <param name="second">The second value to consider.</param>
        /// <returns>
        /// <c>true</c> if the values are equivalent; otherwise, <c>false</c>.
        /// </returns>
        public bool AreEquivalent(ValueTag first, ValueTag second)
        {
            return GetNumber(first) == GetNumber(second);
        }

        /// <summary>
        /// Tries to compute the value number of an instruction.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to number.
        /// </param>
        /// <param name="number">
        /// A value number if a value is found that is equivalent
        /// to <paramref name="instruction"/>; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a value is found that is equivalent
        /// to <paramref name="instruction"/>; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryGetNumber(Instruction instruction, out ValueTag number);

        /// <summary>
        /// Tests if an instruction is equivalent to an instruction.
        /// </summary>
        /// <param name="first">The instruction to consider.</param>
        /// <param name="second">The value to consider.</param>
        /// <returns>
        /// <c>true</c> if the instruction is equivalent to the value; otherwise, <c>false</c>.
        /// </returns>
        public bool AreEquivalent(Instruction first, ValueTag second)
        {
            ValueTag number;
            return TryGetNumber(first, out number) && number == GetNumber(second);
        }

        /// <summary>
        /// Tests if two instructions are equivalent.
        /// </summary>
        /// <param name="first">The first instruction to consider.</param>
        /// <param name="second">The second instruction to consider.</param>
        /// <returns>
        /// <c>true</c> if the instructions are equivalent; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool AreEquivalent(Instruction first, Instruction second)
        {
            return first.Prototype == second.Prototype
                && first.Arguments.SequenceEqual(second.Arguments)
                && IsCopyablePrototype(first.Prototype);
        }

        private static bool IsCopyablePrototype(InstructionPrototype prototype)
        {
            if (prototype is IntrinsicPrototype)
            {
                return IsCopyableIntrinsic((IntrinsicPrototype)prototype);
            }
            else
            {
                return prototype is ConstantPrototype
                    || prototype is CopyPrototype
                    || prototype is DynamicCastPrototype
                    || prototype is GetFieldPointerPrototype
                    || prototype is GetStaticFieldPointerPrototype
                    || prototype is NewDelegatePrototype
                    || prototype is UnboxPrototype;
            }
        }

        private static bool IsCopyableIntrinsic(IntrinsicPrototype intrinsic)
        {
            return ArithmeticIntrinsics.Namespace.IsIntrinsicPrototype(intrinsic);
        }
    }
}
