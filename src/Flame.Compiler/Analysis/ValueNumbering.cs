using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
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
        /// to be equivalent iff 'a' dominates 'b' implies that 'b' can be
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
        /// Tests if an instruction is equivalent to a value.
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
        /// Instructions 'a', 'b' are considered
        /// to be equivalent iff 'a' dominates 'b' implies that 'b' can be
        /// replaced with a copy of the result computed by 'a'.
        /// </summary>
        /// <param name="first">The first instruction to consider.</param>
        /// <param name="second">The second instruction to consider.</param>
        /// <returns>
        /// <c>true</c> if the instructions are equivalent; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool AreEquivalent(Instruction first, Instruction second)
        {
            return object.Equals(first.Prototype, second.Prototype)
                && first.Arguments.Select(GetNumber)
                    .SequenceEqual(second.Arguments.Select(GetNumber))
                && IsCopyablePrototype(first.Prototype);
        }

        /// <summary>
        /// Tells if syntactically equivalent instances of a particular prototype 
        /// are semantically equivalent.
        /// </summary>
        /// <param name="prototype">A prototype to consider.</param>
        /// <returns>
        /// <c>true</c> if syntactically equivalent instances of
        /// <paramref name="prototype"/> are semantically equivalent;
        /// otherwise, <c>false</c>.
        /// </returns>
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
                    || prototype is ReinterpretCastPrototype
                    || prototype is UnboxPrototype;
            }
        }

        /// <summary>
        /// Tells if syntactically equivalent instances of a particular intrinsic
        /// are semantically equivalent.
        /// </summary>
        /// <param name="intrinsic">An intrinsic to consider.</param>
        /// <returns>
        /// <c>true</c> if syntactically equivalent instances of
        /// <paramref name="intrinsic"/> are semantically equivalent;
        /// otherwise, <c>false</c>.
        /// </returns>
        private static bool IsCopyableIntrinsic(IntrinsicPrototype intrinsic)
        {
            return ArithmeticIntrinsics.IsArithmeticIntrinsicPrototype(intrinsic)
                || ArrayIntrinsics.Namespace.IsIntrinsicPrototype(intrinsic, ArrayIntrinsics.Operators.GetLength)
                || ArrayIntrinsics.Namespace.IsIntrinsicPrototype(intrinsic, ArrayIntrinsics.Operators.GetElementPointer)
                || ExceptionIntrinsics.Namespace.IsIntrinsicPrototype(intrinsic, ExceptionIntrinsics.Operators.GetCapturedException);
        }
    }

    /// <summary>
    /// A specialized instruction comparer for instructions that uses value
    /// numbers to more accurately compare instructions. Assumes that all
    /// instructions being compared are defined by the same graph. The
    /// equality relation that arises from this comparer is that of
    /// semantic instruction equivalence, not of syntactic equality.
    /// </summary>
    public sealed class ValueNumberingInstructionComparer : IEqualityComparer<Instruction>
    {
        /// <summary>
        /// Creates an instruction comparer that uses a value numbering
        /// to decide if two instructions are equivalent.
        /// </summary>
        /// <param name="numbering">
        /// The value numbering to use for deciding if instructions are equivalent.
        /// </param>
        public ValueNumberingInstructionComparer(ValueNumbering numbering)
        {
            this.numbering = numbering;
        }

        private ValueNumbering numbering;

        /// <summary>
        /// Tests if two instructions are equivalent in a value
        /// numbering sense.
        /// Instructions 'a', 'b' are considered
        /// to be equivalent iff 'a' dominates 'b' implies that 'b' can be
        /// replaced with a copy of the result computed by 'a'.
        /// </summary>
        /// <param name="a">The first instruction to compare.</param>
        /// <param name="b">The second instruction to compare.</param>
        /// <returns>
        /// <c>true</c> if the instructions are equivalent; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Instruction a, Instruction b)
        {
            return numbering.AreEquivalent(a, b);
        }

        /// <summary>
        /// Computes a hash code for an instruction.
        /// </summary>
        /// <param name="obj">The instruction to hash.</param>
        /// <returns>A hash code.</returns>
        public int GetHashCode(Instruction obj)
        {
            // Compute a hash code for the instruction based on its
            // prototype and the value numbers of its arguments.
            // TODO: this implementation of GetHashCode will always produce
            // a collision for non-copyable instructions (e.g., calls).
            // Is there something we can do about this?

            int hashCode = EnumerableComparer.EmptyHash;
            int argCount = obj.Arguments.Count;
            for (int i = 0; i < argCount; i++)
            {
                hashCode = EnumerableComparer.FoldIntoHashCode(
                    hashCode,
                    numbering.GetNumber(obj.Arguments[i]));
            }
            hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, obj.Prototype);
            return hashCode;
        }
    }

    /// <summary>
    /// An analysis that computes value numbers.
    /// </summary>
    public sealed class ValueNumberingAnalysis : IFlowGraphAnalysis<ValueNumbering>
    {
        private ValueNumberingAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the value numbering analysis.
        /// </summary>
        /// <returns>An instance of the value numbering analysis.</returns>
        public static readonly ValueNumberingAnalysis Instance = new ValueNumberingAnalysis();

        /// <inheritdoc/>
        public ValueNumbering Analyze(FlowGraph graph)
        {
            var numbering = new ValueNumberingImpl();
            foreach (var param in graph.ParameterTags)
            {
                numbering.AddBlockParameter(param);
            }
            foreach (var insn in graph.NamedInstructions)
            {
                numbering.AddInstruction(insn);
            }
            return numbering;
        }

        /// <inheritdoc/>
        public ValueNumbering AnalyzeWithUpdates(
            FlowGraph graph,
            ValueNumbering previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }

        /// <summary>
        /// A simple value numbering implementation.
        /// </summary>
        private sealed class ValueNumberingImpl : ValueNumbering
        {
            public ValueNumberingImpl()
            {
                this.valueNumbers = new Dictionary<ValueTag, ValueTag>();
                this.instructionNumbers = new Dictionary<Instruction, ValueTag>(
                    new ValueNumberingInstructionComparer(this));
            }

            private Dictionary<ValueTag, ValueTag> valueNumbers;
            private Dictionary<Instruction, ValueTag> instructionNumbers;

            /// <summary>
            /// Adds an instruction to this value numbering.
            /// </summary>
            /// <param name="instruction">
            /// The instruction to add.
            /// </param>
            public void AddInstruction(NamedInstruction instruction)
            {
                if (valueNumbers.ContainsKey(instruction))
                {
                    return;
                }

                // TODO: at the moment, we consider all basic block
                // parameters to have their own number. We can do better
                // than this. Concretely, we can use a lattice-based
                // update propagation approach as is used by the constant
                // propagation algorithm. Once lattice-based analyses are
                // abstracted over, we should use that for value numbering
                // instead of this simplistic algorithm.
                //
                // TL;DR: abstract over lattice-based update propagation,
                // then implement better value numbering based on that.

                var graph = instruction.Block.Graph;
                foreach (var arg in instruction.Instruction.Arguments)
                {
                    if (graph.ContainsInstruction(arg))
                    {
                        AddInstruction(graph.GetInstruction(arg));
                    }
                    else
                    {
                        AddBlockParameter(arg);
                    }
                }

                ValueTag number;
                if (!TryGetNumber(instruction.Instruction, out number))
                {
                    number = instruction;
                }
                valueNumbers[instruction] = number;
                instructionNumbers[instruction.Instruction] = number;
            }

            public void AddBlockParameter(ValueTag parameterTag)
            {
                valueNumbers[parameterTag] = parameterTag;
            }

            public override ValueTag GetNumber(ValueTag value)
            {
                return valueNumbers[value];
            }

            public override bool TryGetNumber(Instruction instruction, out ValueTag number)
            {
                return instructionNumbers.TryGetValue(instruction, out number);
            }
        }
    }
}
