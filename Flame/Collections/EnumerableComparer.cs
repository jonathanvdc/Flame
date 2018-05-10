using System.Collections.Generic;
using System.Linq;

namespace Flame.Collections
{
    /// <summary>
    /// An element-wise equality comparer for sequences of values.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements in the sequences to compare.
    /// </typeparam>
    public sealed class EnumerableComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        private EnumerableComparer()
        { }

        /// <summary>
        /// An instance of an enumerable comparer.
        /// </summary>
        public static readonly EnumerableComparer<T> Instance =
            new EnumerableComparer<T>();

        /// <summary>
        /// Tests if two sequences of values are equal.
        /// </summary>
        /// <param name="x">The first sequence to test.</param>
        /// <param name="y">The second sequence to test.</param>
        /// <returns>
        /// <c>true</c> if the sequences are equal element-wise;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            return object.ReferenceEquals(x, y) || (x != null && y != null && x.SequenceEqual<T>(y));
        }

        /// <summary>
        /// Hashes a sequence of values.
        /// </summary>
        /// <param name="obj">The sequence to hash.</param>
        /// <returns>A hash code for the sequence.</returns>
        public int GetHashCode(IEnumerable<T> obj)
        {
            // Simple FNV hash of the sequence. Adapted from
            // http://eternallyconfuzzled.com/tuts/algorithms/jsw_tut_hashing.aspx.

            if (obj == null)
            {
                return 0;
            }

            int hashCode;
            unchecked
            {
                hashCode = (int)2166136261;
                foreach (var item in obj)
                {
                    hashCode = (hashCode * 16777619) ^ item.GetHashCode();
                }
            }
            return hashCode;
        }
    }
}
