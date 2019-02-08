using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for dynamic cast instructions: instructions that
    /// convert one pointer type to another but check that this
    /// conversion is indeed legal; if it is not, then a null pointer
    /// is produced.
    /// </summary>
    public sealed class DynamicCastPrototype : InstructionPrototype
    {
        private DynamicCastPrototype(PointerType targetType)
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
                    "Argument to a dynamic cast has type '" + argType.FullName +
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
                    "Cannot transform a dynamic cast to take non-pointer target type '" +
                    newType.FullName + "'.");
            }
            else
            {
                return Create((PointerType)newType);
            }
        }

        /// <summary>
        /// Gets the input pointer of an instance of this dynamic
        /// cast instruction prototype.
        /// </summary>
        /// <param name="instance">
        /// An instance of this dynamic cast instruction prototype.
        /// </param>
        /// <returns>The input pointer.</returns>
        public ValueTag GetOperand(Instruction instance)
        {
            AssertIsPrototypeOf(instance);
            return instance.Arguments[0];
        }

        /// <summary>
        /// Creates an instance of this dynamic cast instruction
        /// prototype.
        /// </summary>
        /// <param name="operand">
        /// A pointer to cast to another pointer type.
        /// </param>
        /// <returns>
        /// A dynamic cast instruction.
        /// </returns>
        public Instruction Instantiate(ValueTag operand)
        {
            return Instantiate(new ValueTag[] { operand });
        }

        private static readonly InterningCache<DynamicCastPrototype> instanceCache
            = new InterningCache<DynamicCastPrototype>(
                new StructuralDynamicCastPrototypeComparer());

        /// <summary>
        /// Gets or creates a dynamic cast instruction prototype that
        /// converts pointers to a specific pointer type.
        /// </summary>
        /// <param name="targetType">The target pointer type.</param>
        /// <returns>A dynamic cast instruction prototype.</returns>
        public static DynamicCastPrototype Create(PointerType targetType)
        {
            return instanceCache.Intern(new DynamicCastPrototype(targetType));
        }
    }

    internal sealed class StructuralDynamicCastPrototypeComparer
        : IEqualityComparer<DynamicCastPrototype>
    {
        public bool Equals(DynamicCastPrototype x, DynamicCastPrototype y)
        {
            return object.Equals(x.TargetType, y.TargetType);
        }

        public int GetHashCode(DynamicCastPrototype obj)
        {
            return obj.TargetType.GetHashCode();
        }
    }
}
