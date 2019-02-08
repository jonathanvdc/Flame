using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// The prototype for copy instructions.
    /// </summary>
    public sealed class CopyPrototype : InstructionPrototype
    {
        private CopyPrototype(IType resultType)
        {
            this.type = resultType;
        }

        private IType type;

        /// <inheritdoc/>
        public override IType ResultType => type;

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body)
        {
            var inputType = body.Implementation.GetValueType(GetCopiedValue(instance));
            if (inputType.Equals(ResultType))
            {
                return ImmutableList<string>.Empty;
            }
            else
            {
                return ImmutableList<string>.Empty.Add(
                    string.Format(
                        "Input type '{0}' does not match result type '{1}'.",
                        inputType,
                        ResultType));
            }
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            var newType = mapping.MapType(type);
            if (object.ReferenceEquals(newType, type))
            {
                return this;
            }
            else
            {
                return Create(newType);
            }
        }

        /// <summary>
        /// Gets the value copied by an instance of this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The copied value.</returns>
        public ValueTag GetCopiedValue(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            return instruction.Arguments[0];
        }

        /// <summary>
        /// Instantiates this copy prototype.
        /// </summary>
        /// <param name="value">
        /// The value to copy.
        /// </param>
        /// <returns>
        /// A copy instruction.
        /// </returns>
        public Instruction Instantiate(ValueTag value)
        {
            return Instantiate(new ValueTag[] { value });
        }

        private static readonly InterningCache<CopyPrototype> instanceCache
            = new InterningCache<CopyPrototype>(
                new StructuralCopyPrototypeComparer());

        /// <summary>
        /// Gets the copy instruction prototype for a particular result type.
        /// </summary>
        /// <param name="resultType">The result type.</param>
        /// <returns>A copy instruction prototype.</returns>
        public static CopyPrototype Create(IType resultType)
        {
            return instanceCache.Intern(new CopyPrototype(resultType));
        }
    }

    internal sealed class StructuralCopyPrototypeComparer
        : IEqualityComparer<CopyPrototype>
    {
        public bool Equals(CopyPrototype x, CopyPrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType);
        }

        public int GetHashCode(CopyPrototype obj)
        {
            return obj.ResultType.GetHashCode();
        }
    }
}