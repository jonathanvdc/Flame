using System;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A type for n-dimensional arrays of values.
    /// </summary>
    public sealed class ArrayType : ContainerType, IEquatable<ArrayType>
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
        internal ArrayType(IType elementType, int rank)
            : base(
                elementType,
                new ArrayName(elementType.Name.Qualify(), rank),
                new ArrayName(elementType.FullName, rank).Qualify(),
                AttributeMap.Empty)
        {
            this.Rank = rank;
        }

        /// <summary>
        /// Gets this array type's rank, that is, its dimensionality.
        /// </summary>
        /// <returns>The array rank.</returns>
        public int Rank { get; private set; }

        /// <summary>
        /// Checks if this pointer type equals an other pointer type.
        /// </summary>
        /// <param name="other">The other pointer type.</param>
        /// <returns>
        /// <c>true</c> if the pointer types are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ArrayType other)
        {
            return object.ReferenceEquals(this, other)
                || (object.Equals(ElementType, other.ElementType)
                    && Rank == other.Rank);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ArrayType && Equals((ArrayType)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ((object)ElementType).GetHashCode() << 2 ^ Rank.GetHashCode();
        }

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
    }
}