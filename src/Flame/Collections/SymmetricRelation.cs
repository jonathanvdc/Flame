using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// Describes a symmetric relation between values:
    /// a set of two-element sets.
    /// </summary>
    /// <typeparam name="T">
    /// The type of values in the relation.
    /// </typeparam>
    public sealed class SymmetricRelation<T>
    {
        /// <summary>
        /// Creates an empty symmetric relation.
        /// </summary>
        public SymmetricRelation()
        {
            this.relation = new Dictionary<T, HashSet<T>>();
        }

        /// <summary>
        /// Creates an empty symmetric relation.
        /// </summary>
        /// <param name="comparer">The equality comparer to use.</param>
        public SymmetricRelation(IEqualityComparer<T> comparer)
        {
            this.relation = new Dictionary<T, HashSet<T>>(comparer);
        }

        private Dictionary<T, HashSet<T>> relation;

        /// <summary>
        /// Adds a pair of values to the relation.
        /// </summary>
        /// <param name="first">
        /// The first element to relate.
        /// </param>
        /// <param name="second">
        /// The second element to relate.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="first"/> and <paramref name="second"/> are not related yet;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Add(T first, T second)
        {
            GetSetFor(first).Add(second);
            return GetSetFor(second).Add(first);
        }

        /// <summary>
        /// Removes a pair of values from the relation.
        /// </summary>
        /// <param name="first">
        /// A first element.
        /// </param>
        /// <param name="second">
        /// A second element.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="first"/> and <paramref name="second"/> were related;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Remove(T first, T second)
        {
            GetSetFor(first).Remove(second);
            return GetSetFor(second).Remove(first);
        }

        /// <summary>
        /// Tests if two elements are related.
        /// </summary>
        /// <param name="first">
        /// A first element.
        /// </param>
        /// <param name="second">
        /// A second element.
        /// </param>
        /// <returns>
        /// <c>true</c> if the elements are related under this relation;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(T first, T second)
        {
            HashSet<T> firstSet;
            if (relation.TryGetValue(first, out firstSet))
            {
                return firstSet.Contains(second);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the set of all values related to a particular value.
        /// </summary>
        /// <param name="value">The value to examine.</param>
        /// <returns>The set of all related values.</returns>
        public IEnumerable<T> GetAll(T value)
        {
            return GetSetFor(value);
        }

        private HashSet<T> GetSetFor(T value)
        {
            HashSet<T> valueSet;
            if (relation.TryGetValue(value, out valueSet))
            {
                return valueSet;
            }
            else
            {
                valueSet = new HashSet<T>(relation.Comparer);
                relation[value] = valueSet;
                return valueSet;
            }
        }
    }
}
