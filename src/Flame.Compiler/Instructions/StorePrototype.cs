using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for store instructions that set the value of
    /// a pointer's pointee.
    /// </summary>
    public sealed class StorePrototype : InstructionPrototype
    {
        private StorePrototype(IType elementType, bool isVolatile, Alignment alignment)
        {
            this.elemType = elementType;
            this.IsVolatile = isVolatile;
            this.Alignment = alignment;
        }

        private IType elemType;

        /// <summary>
        /// Tests if instances of this store prototype are volatile operations.
        /// Volatile operations may not be reordered with regard to each other.
        /// </summary>
        /// <value><c>true</c> if this is a prototype for volatile stores; otherwise, <c>false</c>.</value>
        public bool IsVolatile { get; private set; }

        /// <summary>
        /// Gets the pointer alignment of pointers written to by this prototype.
        /// </summary>
        /// <value>The pointer alignment of pointers written to by this prototype.</value>
        public Alignment Alignment { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType => elemType;

        /// <inheritdoc/>
        public override int ParameterCount => 2;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            var errors = new List<string>();

            var ptrType = body.Implementation.GetValueType(GetPointer(instance)) as PointerType;
            if (ptrType == null)
            {
                errors.Add("Target of store operation must be a pointer type.");
            }
            else if (!ptrType.ElementType.Equals(elemType))
            {
                errors.Add(
                    string.Format(
                        "Pointee type '{0}' of store target pointer should " +
                        "have been '{1}'.",
                        ptrType.ElementType.FullName,
                        elemType.FullName));
            }

            var valueType = body.Implementation.GetValueType(GetValue(instance));
            if (!valueType.Equals(elemType))
            {
                errors.Add(
                    string.Format(
                        "Type of value stored in pointer was '{0}' but should " +
                        "have been '{1}'.",
                        valueType.FullName,
                        elemType.FullName));
            }

            return errors;
        }

        /// <summary>
        /// Gets a variant of this store prototype with a particular volatility.
        /// </summary>
        /// <param name="isVolatile">The volatility to assign to the store.</param>
        /// <returns>
        /// A store prototype that copies all properties from this one, except for
        /// its volatility, which is set to <paramref name="isVolatile"/>.
        /// </returns>
        public StorePrototype WithVolatility(bool isVolatile)
        {
            if (IsVolatile == isVolatile)
            {
                return this;
            }
            else
            {
                return Create(elemType, isVolatile, Alignment);
            }
        }

        /// <summary>
        /// Gets a variant of this store prototype with a particular alignment.
        /// </summary>
        /// <param name="alignment">The alignment to assign to the store.</param>
        /// <returns>
        /// A store prototype that copies all properties from this one, except for
        /// its alignment, which is set to <paramref name="alignment"/>.
        /// </returns>
        public StorePrototype WithAlignment(Alignment alignment)
        {
            if (Alignment == alignment)
            {
                return this;
            }
            else
            {
                return Create(elemType, IsVolatile, alignment);
            }
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            var newType = mapping.MapType(elemType);
            if (object.ReferenceEquals(newType, elemType))
            {
                return this;
            }
            else
            {
                return Create(newType, IsVolatile, Alignment);
            }
        }

        /// <summary>
        /// Gets the pointer to which a store is performed by
        /// an instance of this prototype.
        /// </summary>
        /// <param name="instance">
        /// An instance of this prototype.
        /// </param>
        /// <returns>
        /// The pointer whose pointee's value is replaced.
        /// </returns>
        public ValueTag GetPointer(Instruction instance)
        {
            AssertIsPrototypeOf(instance);
            return instance.Arguments[0];
        }

        /// <summary>
        /// Gets the value with which a store instruction's
        /// pointee is replaced.
        /// </summary>
        /// <param name="instance">
        /// An instance of this prototype.
        /// </param>
        /// <returns>
        /// The stored value.
        /// </returns>
        public ValueTag GetValue(Instruction instance)
        {
            AssertIsPrototypeOf(instance);
            return instance.Arguments[1];
        }

        /// <summary>
        /// Creates an instance of this store prototype.
        /// </summary>
        /// <param name="pointer">
        /// A pointer to the value to replace.
        /// </param>
        /// <param name="value">
        /// The value to store in the pointer's pointee.
        /// </param>
        /// <returns>A store instruction.</returns>
        public Instruction Instantiate(ValueTag pointer, ValueTag value)
        {
            return Instantiate(new ValueTag[] { pointer, value });
        }

        private static readonly InterningCache<StorePrototype> instanceCache
            = new InterningCache<StorePrototype>(
                new StructuralStorePrototypeComparer());

        /// <summary>
        /// Gets or creates a store instruction prototype for a particular
        /// element type.
        /// </summary>
        /// <param name="elementType">
        /// The type of element to store in a pointer.
        /// </param>
        /// <param name="isVolatile">
        /// Tells if instances of the store prototype are volatile operations.
        /// Volatile operations may not be reordered with regard to each other.
        /// </param>
        /// <param name="alignment">
        /// The pointer alignment of pointers written to by the prototype.
        /// </param>
        /// <returns>
        /// A store instruction prototype.
        /// </returns>
        public static StorePrototype Create(
            IType elementType,
            bool isVolatile = false,
            Alignment alignment = default(Alignment))
        {
            return instanceCache.Intern(new StorePrototype(elementType, isVolatile, alignment));
        }
    }

    internal sealed class StructuralStorePrototypeComparer
        : IEqualityComparer<StorePrototype>
    {
        public bool Equals(StorePrototype x, StorePrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType)
                && x.IsVolatile == y.IsVolatile
                && x.Alignment == y.Alignment;
        }

        public int GetHashCode(StorePrototype obj)
        {
            var hash = EnumerableComparer.EmptyHash;
            hash = EnumerableComparer.FoldIntoHashCode(hash, obj.ResultType);
            hash = EnumerableComparer.FoldIntoHashCode(hash, obj.IsVolatile);
            hash = EnumerableComparer.FoldIntoHashCode(hash, obj.Alignment);
            return hash;
        }
    }
}
