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
        public override ExceptionSpecification ExceptionSpecification
            => ExceptionSpecification.NoThrow;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            return EmptyArray<string>.Value;
        }

        private static readonly InterningCache<AllocaPrototype> instanceCache
            = new InterningCache<AllocaPrototype>(
                new StructuralAllocaPrototypeComparer());

        /// <summary>
        /// Gets the alloca instruction prototype for a particular result type.
        /// </summary>
        /// <param name="resultType">The result type.</param>
        /// <returns>An alloca instruction prototype.</returns>
        public static AllocaPrototype Create(IType resultType)
        {
            return instanceCache.Intern(new AllocaPrototype(resultType));
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