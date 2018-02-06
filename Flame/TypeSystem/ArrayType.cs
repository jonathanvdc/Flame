using System;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A type for n-dimensional arrays of values.
    /// </summary>
    public sealed class ArrayType : ContainerType
    {
        /// <summary>
        /// Creates an array type from an element type and a rank.
        /// </summary>
        /// <param name="elementType">
        /// The type of element referred to by the array.
        /// </param>
        /// <param name="rank">
        /// The array type's rank, that is, its dimensionality.
        /// </param>
        private ArrayType(IType elementType, int rank)
            : base(elementType)
        {
            this.Rank = rank;
        }

        /// <summary>
        /// Gets this array type's rank, that is, its dimensionality.
        /// </summary>
        /// <returns>The array rank.</returns>
        public int Rank { get; private set; }

        /// <inheritdoc/>
        public override ContainerType WithElementType(IType newElementType)
        {
            if (object.ReferenceEquals(ElementType, newElementType))
            {
                return this;
            }
            else
            {
                return newElementType.MakeArrayType(Rank);
            }
        }

        // This cache interns all array types: if two ArrayType instances
        // (in the wild, not in this private set-up logic) have equal element
        // types and ranks, then they are *referentially* equal.
        private static InterningCache<ArrayType> instanceCache
            = new InterningCache<ArrayType>(
                new StructuralArrayTypeComparer(),
                InitializeInstance);

        /// <summary>
        /// Creates an array type of a particular rank that has a
        /// type as element.
        /// </summary>
        /// <param name="type">
        /// The type of values referred to by the array type.
        /// </param>
        /// <param name="rank">
        /// The array type's rank, that is, its dimensionality.
        /// </param>
        /// <returns>An array type.</returns>
        internal static ArrayType Create(IType type, int rank)
        {
            return instanceCache.Intern(new ArrayType(type, rank));
        }

        private static ArrayType InitializeInstance(ArrayType instance)
        {
            instance.Initialize(
                new ArrayName(instance.ElementType.Name.Qualify(), instance.Rank),
                new ArrayName(instance.ElementType.FullName, instance.Rank).Qualify(),
                AttributeMap.Empty);
            return instance;
        }
    }

    internal sealed class StructuralArrayTypeComparer : IEqualityComparer<ArrayType>
    {
        public bool Equals(ArrayType x, ArrayType y)
        {
            return object.Equals(x.ElementType, y.ElementType)
                && x.Rank.Equals(y.Rank);
        }

        public int GetHashCode(ArrayType obj)
        {
            return ((object)obj.ElementType).GetHashCode() << 2 ^ obj.Rank.GetHashCode();
        }
    }
}