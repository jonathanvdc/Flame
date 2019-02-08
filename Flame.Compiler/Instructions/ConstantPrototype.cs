using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for instructions that produce a constant value.
    /// </summary>
    public sealed class ConstantPrototype : InstructionPrototype
    {
        private ConstantPrototype(Constant value, IType type)
        {
            this.Value = value;
            this.type = type;
        }

        private IType type;

        /// <summary>
        /// Gets the constant value produced by instances of this
        /// prototype.
        /// </summary>
        /// <returns>A constant value.</returns>
        public Constant Value { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType => type;

        /// <inheritdoc/>
        public override int ParameterCount => 0;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            return EmptyArray<string>.Value;
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
                return Create(Value, newType);
            }
        }

        /// <summary>
        /// Creates an instance of this prototype.
        /// </summary>
        /// <returns>A constant instruction.</returns>
        public Instruction Instantiate()
        {
            return Instantiate(EmptyArray<ValueTag>.Value);
        }

        private static readonly InterningCache<ConstantPrototype> instanceCache
            = new InterningCache<ConstantPrototype>(
                new StructuralConstantPrototypeComparer());

        /// <summary>
        /// Gets or creates a constant instruction prototype.
        /// </summary>
        /// <param name="value">The constant value to produce.</param>
        /// <param name="type">The result type of prototype instances.</param>
        /// <returns>A constant instruction prototype.</returns>
        public static ConstantPrototype Create(Constant value, IType type)
        {
            return instanceCache.Intern(new ConstantPrototype(value, type));
        }
    }

    internal sealed class StructuralConstantPrototypeComparer
        : IEqualityComparer<ConstantPrototype>
    {
        public bool Equals(ConstantPrototype x, ConstantPrototype y)
        {
            return x.Value.Equals(y.Value)
                && object.Equals(x.ResultType, y.ResultType);
        }

        public int GetHashCode(ConstantPrototype obj)
        {
            return (obj.Value.GetHashCode() << 3) ^ obj.ResultType.GetHashCode();
        }
    }
}