using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for load instructions, which dereference pointers.
    /// </summary>
    public sealed class LoadPrototype : InstructionPrototype
    {
        private LoadPrototype(IType elementType)
        {
            this.elemType = elementType;
        }

        private IType elemType;

        /// <inheritdoc/>
        public override IType ResultType => elemType;

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override ExceptionSpecification ExceptionSpecification
            // TODO: refine this somehow to reflect the fact that this can
            // only go wrong if the loaded pointer is null (or a dangling
            // pointer for transient pointers).
            => ExceptionSpecification.ThrowAny;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            var ptrType = body.Implementation.GetValueType(GetPointer(instance)) as PointerType;
            if (ptrType == null)
            {
                return new string[]
                {
                    "Target of load operation must be a pointer type."
                };
            }
            else if (!ptrType.ElementType.Equals(elemType))
            {
                return new string[]
                {
                    string.Format(
                        "Pointee type '{0}' of load argument should " +
                        "have been '{1}'.",
                        ptrType.ElementType.FullName,
                        elemType.FullName)
                };
            }
            else
            {
                return EmptyArray<string>.Value;
            }
        }

        /// <summary>
        /// Gets the pointer that is loaded by an instance of this
        /// prototype.
        /// </summary>
        /// <param name="instance">
        /// An instance of this prototype.
        /// </param>
        /// <returns>
        /// The pointer whose pointee is loaded.
        /// </returns>
        public ValueTag GetPointer(Instruction instance)
        {
            AssertIsPrototypeOf(instance);
            return instance.Arguments[0];
        }

        private static readonly InterningCache<LoadPrototype> instanceCache
            = new InterningCache<LoadPrototype>(
                new StructuralLoadPrototypeComparer());

        /// <summary>
        /// Gets or creates a load instruction prototype for a particular
        /// element type.
        /// </summary>
        /// <param name="elementType">
        /// The type of element to load from a pointer.
        /// </param>
        /// <returns>
        /// A load instruction prototype.
        /// </returns>
        public static LoadPrototype Create(IType elementType)
        {
            return instanceCache.Intern(new LoadPrototype(elementType));
        }
    }

    internal sealed class StructuralLoadPrototypeComparer
        : IEqualityComparer<LoadPrototype>
    {
        public bool Equals(LoadPrototype x, LoadPrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType);
        }

        public int GetHashCode(LoadPrototype obj)
        {
            return obj.ResultType.GetHashCode();
        }
    }
}