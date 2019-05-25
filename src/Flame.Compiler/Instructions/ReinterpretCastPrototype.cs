using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for reinterpret cast instructions: instructions that
    /// convert one pointer type to another and are free to assume that
    /// this conversion will always succeed.
    /// </summary>
    public sealed class ReinterpretCastPrototype : InstructionPrototype
    {
        private ReinterpretCastPrototype(PointerType targetType)
        {
            this.TargetType = targetType;
        }

        /// <summary>
        /// Gets the pointer type to cast the input to.
        /// </summary>
        /// <returns>The target pointer type.</returns>
        public PointerType TargetType { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType => TargetType;

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body)
        {
            var argType = body.Implementation.GetValueType(GetOperand(instance));
            if (!(argType is PointerType))
            {
                return new string[]
                {
                    "Argument to a reinterpret cast has type '" + argType.FullName +
                    "', but should have had a pointer type."
                };
            }
            else
            {
                return EmptyArray<string>.Value;
            }
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            var newType = mapping.MapType(TargetType);
            if (object.ReferenceEquals(newType, TargetType))
            {
                return this;
            }
            else if (!(newType is PointerType))
            {
                throw new InvalidOperationException(
                    "Cannot transform a reinterpret cast to take non-pointer target type '" +
                    newType.FullName + "'.");
            }
            else
            {
                return Create((PointerType)newType);
            }
        }

        /// <summary>
        /// Gets the input pointer of an instance of this reinterpret-
        /// cast instruction prototype.
        /// </summary>
        /// <param name="instance">
        /// An instance of this reinterpret cast instruction prototype.
        /// </param>
        /// <returns>The input pointer.</returns>
        public ValueTag GetOperand(Instruction instance)
        {
            AssertIsPrototypeOf(instance);
            return instance.Arguments[0];
        }

        /// <summary>
        /// Creates an instance of this reinterpret cast instruction
        /// prototype.
        /// </summary>
        /// <param name="operand">
        /// A pointer to cast to another pointer type.
        /// </param>
        /// <returns>
        /// A reinterpret cast instruction.
        /// </returns>
        public Instruction Instantiate(ValueTag operand)
        {
            return Instantiate(new ValueTag[] { operand });
        }

        private static readonly InterningCache<ReinterpretCastPrototype> instanceCache
            = new InterningCache<ReinterpretCastPrototype>(
                new StructuralReinterpretCastPrototypeComparer());

        /// <summary>
        /// Gets or creates a reinterpret cast instruction prototype that
        /// converts pointers to a specific pointer type.
        /// </summary>
        /// <param name="targetType">The target pointer type.</param>
        /// <returns>A reinterpret cast instruction prototype.</returns>
        public static ReinterpretCastPrototype Create(PointerType targetType)
        {
            return instanceCache.Intern(new ReinterpretCastPrototype(targetType));
        }
    }

    internal sealed class StructuralReinterpretCastPrototypeComparer
        : IEqualityComparer<ReinterpretCastPrototype>
    {
        public bool Equals(ReinterpretCastPrototype x, ReinterpretCastPrototype y)
        {
            return object.Equals(x.TargetType, y.TargetType);
        }

        public int GetHashCode(ReinterpretCastPrototype obj)
        {
            return obj.TargetType.GetHashCode();
        }
    }
}
