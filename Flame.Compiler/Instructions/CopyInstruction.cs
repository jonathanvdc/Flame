using System.Collections.Generic;
using Flame.Collections;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// An instruction that outputs an existing value without modifying
    /// it in any way. This is equivalent to a shallow copy.
    /// </summary>
    public sealed class CopyInstruction : Instruction
    {
        internal CopyInstruction(CopyInstructionPrototype proto, ValueTag value)
        {
            this.proto = proto;
            this.Value = value;
        }

        /// <summary>
        /// Creates a copy instruction from a result type and a value.
        /// </summary>
        /// <param name="type">The type of the copy instruction's input and output.</param>
        /// <param name="value">The value to copy.</param>
        public CopyInstruction(IType type, ValueTag value)
            : this(CopyInstructionPrototype.Create(type), value)
        { }

        private CopyInstructionPrototype proto;

        /// <summary>
        /// Gets the value that is copied by this instruction.
        /// </summary>
        /// <returns>The value that is copied.</returns>
        public ValueTag Value { get; private set; }

        /// <inheritdoc/>
        public override InstructionPrototype Prototype => proto;

        /// <inheritdoc/>
        public override IReadOnlyList<ValueTag> Arguments => new ValueTag[] { Value };
    }

    /// <summary>
    /// The prototype for copy instructions.
    /// </summary>
    public sealed class CopyInstructionPrototype : InstructionPrototype
    {
        private CopyInstructionPrototype(IType resultType)
        {
            this.type = resultType;
        }

        private IType type;

        /// <inheritdoc/>
        public override IType ResultType => type;

        /// <inheritdoc/>
        public override Instruction Instantiate(IReadOnlyList<ValueTag> arguments)
        {
            ContractHelpers.Assert(arguments.Count == 1, "Copy instructions take exactly one argument.");
            return new CopyInstruction(this, arguments[0]);
        }

        private static readonly InterningCache<CopyInstructionPrototype> instanceCache
            = new InterningCache<CopyInstructionPrototype>(
                new StructuralCopyInstructionPrototypeComparer());

        /// <summary>
        /// Gets the copy instruction prototype for a particular result type.
        /// </summary>
        /// <param name="resultType">The result type.</param>
        /// <returns>A copy instruction prototype.</returns>
        public static CopyInstructionPrototype Create(IType resultType)
        {
            return instanceCache.Intern(new CopyInstructionPrototype(resultType));
        }
    }

    internal sealed class StructuralCopyInstructionPrototypeComparer
        : IEqualityComparer<CopyInstructionPrototype>
    {
        public bool Equals(CopyInstructionPrototype x, CopyInstructionPrototype y)
        {
            return x.ResultType.Equals(y.ResultType);
        }

        public int GetHashCode(CopyInstructionPrototype obj)
        {
            return obj.ResultType.GetHashCode();
        }
    }
}