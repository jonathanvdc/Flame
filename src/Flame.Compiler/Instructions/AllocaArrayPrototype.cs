using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for alloca-array instructions, which allocate a
    /// variable-length array of values on the stack.
    /// </summary>
    public sealed class AllocaArrayPrototype : InstructionPrototype
    {
        private AllocaArrayPrototype(IType elementType)
        {
            this.ElementType = elementType;
        }

        /// <summary>
        /// Gets the type of element to allocate storage for.
        /// </summary>
        /// <returns>The type of element.</returns>
        public IType ElementType { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType => ElementType.MakePointerType(PointerKind.Transient);

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            return EmptyArray<string>.Value;
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            var newType = mapping.MapType(ElementType);
            if (object.ReferenceEquals(newType, ElementType))
            {
                return this;
            }
            else
            {
                return Create(newType);
            }
        }

        /// <summary>
        /// Gets the number of elements allocated by an instance
        /// of this prototype.
        /// </summary>
        /// <param name="instance">
        /// An alloca-array instruction.
        /// </param>
        /// <returns>
        /// The number of elements allocated by the instruction.
        /// </returns>
        public ValueTag GetElementCount(Instruction instance)
        {
            AssertIsPrototypeOf(instance);
            return instance.Arguments[0];
        }

        /// <summary>
        /// Instantiates this prototype.
        /// </summary>
        /// <param name="elementCount">
        /// The number of elements to allocate storage for.
        /// </param>
        /// <returns>
        /// An alloca-array instruction.
        /// </returns>
        public Instruction Instantiate(ValueTag elementCount)
        {
            return Instantiate(new ValueTag[] { elementCount });
        }

        private static readonly InterningCache<AllocaArrayPrototype> instanceCache
            = new InterningCache<AllocaArrayPrototype>(
                new StructuralAllocaArrayPrototypeComparer());

        /// <summary>
        /// Gets the alloca-array instruction prototype for a particular result type.
        /// </summary>
        /// <param name="resultType">The result type.</param>
        /// <returns>An alloca-array instruction prototype.</returns>
        public static AllocaArrayPrototype Create(IType resultType)
        {
            return instanceCache.Intern(new AllocaArrayPrototype(resultType));
        }
    }

    internal sealed class StructuralAllocaArrayPrototypeComparer
        : IEqualityComparer<AllocaArrayPrototype>
    {
        public bool Equals(AllocaArrayPrototype x, AllocaArrayPrototype y)
        {
            return object.Equals(x.ElementType, y.ElementType);
        }

        public int GetHashCode(AllocaArrayPrototype obj)
        {
            return obj.ElementType.GetHashCode();
        }
    }
}