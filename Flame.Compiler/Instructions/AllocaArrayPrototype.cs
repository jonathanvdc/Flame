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
        public override ExceptionSpecification ExceptionSpecification
            => ExceptionSpecification.NoThrow;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            return EmptyArray<string>.Value;
        }
        private static readonly InterningCache<AllocaArrayPrototype> instanceCache
            = new InterningCache<AllocaArrayPrototype>(
                new StructuralAllocaArrayPrototypeComparer());

        /// <summary>
        /// Gets the alloca instruction prototype for a particular result type.
        /// </summary>
        /// <param name="resultType">The result type.</param>
        /// <returns>A alloca instruction prototype.</returns>
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
            return x.ElementType.Equals(y.ElementType);
        }

        public int GetHashCode(AllocaArrayPrototype obj)
        {
            return obj.ElementType.GetHashCode();
        }
    }
}