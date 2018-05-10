using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// A data structure that memoizes lookups by key in a
    /// data structure.
    /// </summary>
    /// <typeparam name="TContainer">
    /// The type of data structure to look up values in.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of key with which values can be looked up.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of value to look up.
    /// </typeparam>
    public sealed class Index<TContainer, TKey, TValue>
        where TContainer : class
    {
        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="getElements">
        /// Takes a container and returns its contents as a sequence
        /// of key-value pairs. A single key may occur more than once
        /// in the output.
        /// </param>
        public Index(
            Func<TContainer, IEnumerable<KeyValuePair<TKey, TValue>>> getElements)
            : this(getElements, EqualityComparer<TKey>.Default)
        { }

        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="getElements">
        /// Takes a container and returns its contents as a sequence
        /// of key-value pairs. A single key may occur more than once
        /// in the output.
        /// </param>
        /// <param name="keyComparer">
        /// A comparer for keys.
        /// </param>
        public Index(
            Func<TContainer, IEnumerable<KeyValuePair<TKey, TValue>>> getElements,
            IEqualityComparer<TKey> keyComparer)
        {
            this.getElements = getElements;
            this.keyComparer = keyComparer;
            this.indexCache = new WeakCache<TContainer, Dictionary<TKey, List<TValue>>>();
        }

        private Func<TContainer, IEnumerable<KeyValuePair<TKey, TValue>>> getElements;
        private WeakCache<TContainer, Dictionary<TKey, List<TValue>>> indexCache;
        private IEqualityComparer<TKey> keyComparer;

        /// <summary>
        /// Gets a list of all values in a container that are tagged
        /// with a particular key.
        /// </summary>
        /// <param name="container">The container to search.</param>
        /// <param name="key">The key to look for.</param>
        /// <returns>A list of values.</returns>
        public IReadOnlyList<TValue> GetAll(TContainer container, TKey key)
        {
            var dictionary = indexCache.Get(container, IndexContainer);
            List<TValue> results;
            if (dictionary.TryGetValue(key, out results))
            {
                return results;
            }
            else
            {
                return EmptyArray<TValue>.Value;
            }
        }

        private Dictionary<TKey, List<TValue>> IndexContainer(TContainer container)
        {
            var results = new Dictionary<TKey, List<TValue>>(keyComparer);
            foreach (var kvPair in getElements(container))
            {
                List<TValue> elemList;
                if (!results.TryGetValue(kvPair.Key, out elemList))
                {
                    elemList = new List<TValue>();
                    results[kvPair.Key] = elemList;
                }
                elemList.Add(kvPair.Value);
            }
            return results;
        }
    }
}
