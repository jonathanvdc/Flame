using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for alloca instructions, which allocate a single value
    /// on the stack.
    /// </summary>
    public sealed class AllocaPrototype : InstructionPrototype
    {
        private AllocaPrototype(IType elementType)
        {
            this.ElementType = elementType;
        }

        /// <summary>
        /// Gets the type of element to allocate storage for.
        /// </summary>
        /// <returns>The type of element.</returns>
        public IType ElementType { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType => ElementType.MakePointerType(PointerKind.Reference);

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
        /// Instantiates this prototype.
        /// </summary>
        /// <returns>An alloca instruction.</returns>
        public Instruction Instantiate()
        {
            return Instantiate(EmptyArray<ValueTag>.Value);
        }

        private static readonly InterningCache<AllocaPrototype> instanceCache
            = new InterningCache<AllocaPrototype>(
                new StructuralAllocaPrototypeComparer());

        /// <summary>
        /// Gets the alloca instruction prototype for a particular result type.
        /// </summary>
        /// <param name="elementType">The type of value to allocate storage for.</param>
        /// <returns>An alloca instruction prototype.</returns>
        public static AllocaPrototype Create(IType elementType)
        {
            return instanceCache.Intern(new AllocaPrototype(elementType));
        }
    }

    internal sealed class StructuralAllocaPrototypeComparer
        : IEqualityComparer<AllocaPrototype>
    {
        public bool Equals(AllocaPrototype x, AllocaPrototype y)
        {
            return object.Equals(x.ElementType, y.ElementType);
        }

        public int GetHashCode(AllocaPrototype obj)
        {
            return obj.ElementType.GetHashCode();
        }
    }
}