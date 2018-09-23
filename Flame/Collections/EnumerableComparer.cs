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
        /// <summary>
        /// Creates an equality comparer for sequences from an equality
        /// comparer for elements.
        /// </summary>
        /// <param name="elementComparer"></param>
        public EnumerableComparer(IEqualityComparer<T> elementComparer)
        {
            this.ElementComparer = elementComparer;
        }

        /// <summary>
        /// Gets the equality comparer for sequence elements.
        /// </summary>
        /// <returns>The equality comparer for elements.</returns>
        public IEqualityComparer<T> ElementComparer { get; private set; }

        /// <summary>
        /// An instance of an enumerable comparer based on the
        /// default element comparer.
        /// </summary>
        public static readonly EnumerableComparer<T> Default =
            new EnumerableComparer<T>(EqualityComparer<T>.Default);

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
            return object.ReferenceEquals(x, y)
                || (x != null && y != null && x.SequenceEqual<T>(y, ElementComparer));
        }

        /// <summary>
        /// Hashes a sequence of values.
        /// </summary>
        /// <param name="obj">The sequence to hash.</param>
        /// <returns>A hash code for the sequence.</returns>
        public int GetHashCode(IEnumerable<T> obj)
        {
            return EnumerableComparer.HashEnumerable(obj, ElementComparer);
        }
    }

    /// <summary>
    /// An element-wise equality comparer for sequences of values.
    /// </summary>
    public static class EnumerableComparer
    {
        /// <summary>
        /// Hashes a sequence of elements.
        /// </summary>
        /// <param name="sequence">The sequence to hash.</param>
        /// <param name="elementComparer">
        /// An equality comparer for elements of the sequence.
        /// </param>
        /// <typeparam name="T">
        /// The type of element in the sequence.
        /// </typeparam>
        /// <returns>A hash code for the entire sequence.</returns>
        public static int HashEnumerable<T>(
            IEnumerable<T> sequence,
            IEqualityComparer<T> elementComparer)
        {
            // Simple FNV hash of the sequence. Adapted from
            // http://eternallyconfuzzled.com/tuts/algorithms/jsw_tut_hashing.aspx.

            if (sequence == null)
            {
                return 0;
            }

            int hashCode = EmptyHash;
            foreach (var item in sequence)
            {
                hashCode = FoldIntoHashCode(hashCode, elementComparer.GetHashCode(item));
            }
            return hashCode;
        }

        /// <summary>
        /// Hashes a sequence of elements.
        /// </summary>
        /// <param name="sequence">The sequence to hash.</param>
        /// <typeparam name="T">
        /// The type of element in the sequence.
        /// </typeparam>
        /// <returns>A hash code for the entire sequence.</returns>
        public static int HashEnumerable<T>(
            IEnumerable<T> sequence)
        {
            return HashEnumerable<T>(sequence, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Hashes an unordered set of elements.
        /// </summary>
        /// <param name="sequence">The unordered set to hash.</param>
        /// <param name="elementComparer">
        /// An equality comparer for elements of the set.
        /// </param>
        /// <typeparam name="T">
        /// The type of element in the set.
        /// </typeparam>
        /// <returns>
        /// An ordering-independent hash code for the entire set.
        /// </returns>
        public static int HashUnorderedSet<T>(
            IEnumerable<T> sequence,
            IEqualityComparer<T> elementComparer)
        {
            var set = new HashSet<T>(sequence, elementComparer);
            return HashSet<T>.CreateSetComparer().GetHashCode(set);
        }

        /// <summary>
        /// Hashes an unordered set of elements.
        /// </summary>
        /// <param name="sequence">The unordered set to hash.</param>
        /// <typeparam name="T">
        /// The type of element in the set.
        /// </typeparam>
        /// <returns>
        /// An ordering-independent hash code for the entire set.
        /// </returns>
        public static int HashUnorderedSet<T>(
            IEnumerable<T> sequence)
        {
            return HashUnorderedSet<T>(sequence, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// The hash code for an empty sequence.
        /// </summary>
        public const int EmptyHash = unchecked((int)2166136261);

        /// <summary>
        /// Folds a hash code for an element into a hash code for a sequence.
        /// </summary>
        /// <param name="hashCode">
        /// A hash code for a sequence without the element.
        /// </param>
        /// <param name="elementHashCode">
        /// A hash code for an element.
        /// </param>
        /// <returns>
        /// A hash code for a sequence that includes the element.
        /// </returns>
        public static int FoldIntoHashCode(int hashCode, int elementHashCode)
        {
            return (hashCode * 16777619) ^ elementHashCode;
        }

        /// <summary>
        /// Folds a hash code for an element into a hash code for a sequence.
        /// </summary>
        /// <param name="hashCode">
        /// A hash code for a sequence without the element.
        /// </param>
        /// <param name="element">
        /// An element whose hash code is to be folded into the hash
        /// code for the sequence.
        /// </param>
        /// <returns>
        /// A hash code for a sequence that includes the element.
        /// </returns>
        public static int FoldIntoHashCode<T>(int hashCode, T element)
        {
            return FoldIntoHashCode(hashCode, element.GetHashCode());
        }
    }
}
