using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for instructions that unbox boxed value types.
    /// Unbox instructions take box pointers and turn them into
    /// reference pointers to their contents.
    /// </summary>
    public sealed class UnboxPrototype : InstructionPrototype
    {
        private UnboxPrototype(PointerType ptrType)
        {
            this.refType = ptrType;
        }

        private PointerType refType;

        /// <summary>
        /// Gets the type of value to unbox.
        /// </summary>
        public IType ElementType => refType.ElementType;

        /// <inheritdoc/>
        public override IType ResultType => refType;

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            var valType = body.Implementation.GetValueType(GetBoxPointer(instance)) as PointerType;
            if (valType == null || valType.Kind != PointerKind.Box)
            {
                return new[]
                {
                    "Unbox instruction must always take a box pointer value as argument " +
                    " but is passed a value of type '" + valType + "'."
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
            return Create(mapping.MapType(ResultType));
        }

        /// <summary>
        /// Gets the box pointer that is unboxed by an instance of this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The box pointer.</returns>
        public ValueTag GetBoxPointer(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            return instruction.Arguments[0];
        }

        /// <summary>
        /// Instantiates this unbox instruction prototype.
        /// </summary>
        /// <param name="value">
        /// A box pointer to unbox.
        /// </param>
        /// <returns>
        /// An unbox instruction.
        /// </returns>
        public Instruction Instantiate(ValueTag value)
        {
            return Instantiate(new ValueTag[] { value });
        }

        private static readonly InterningCache<UnboxPrototype> instanceCache
            = new InterningCache<UnboxPrototype>(
                new StructuralUnboxPrototypeComparer());

        /// <summary>
        /// Gets the unbox instruction prototype for a particular result type.
        /// </summary>
        /// <param name="elementType">The type of the unboxed value to produce.</param>
        /// <returns>A unbox instruction prototype.</returns>
        public static UnboxPrototype Create(IType elementType)
        {
            return instanceCache.Intern(new UnboxPrototype(elementType.MakePointerType(PointerKind.Reference)));
        }
    }

    internal sealed class StructuralUnboxPrototypeComparer
        : IEqualityComparer<UnboxPrototype>
    {
        public bool Equals(UnboxPrototype x, UnboxPrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType);
        }

        public int GetHashCode(UnboxPrototype obj)
        {
            return obj.ResultType.GetHashCode();
        }
    }
}
