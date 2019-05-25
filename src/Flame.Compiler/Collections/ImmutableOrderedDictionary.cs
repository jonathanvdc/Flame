using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flame.Collections
{
    /// <summary>
    /// An immutable dictionary that iterates over elements in the
    /// order they were added.
    /// </summary>
    public static class ImmutableOrderedDictionary
    {
        /// <summary>
        /// Creates an immutable ordered dictionary.
        /// </summary>
        /// <returns>An immutable ordered dictionary.</returns>
        public static ImmutableOrderedDictionary<TKey, TValue> Create<TKey, TValue>()
        {
            return ImmutableOrderedDictionary<TKey, TValue>.Empty;
        }
    }

    /// <summary>
    /// An immutable dictionary that iterates over elements in the
    /// order they were added.
    /// </summary>
    public struct ImmutableOrderedDictionary<TKey, TValue>
        : IImmutableDictionary<TKey, TValue>
    {
        private ImmutableOrderedDictionary(
            ImmutableDictionary<TKey, TValue> innerDictionary,
            ImmutableList<TKey> orderedKeyList)
        {
            this.innerDictionary = innerDictionary;
            this.orderedKeyList = orderedKeyList;
        }

        /// <summary>
        /// An empty immutable ordered dictionary.
        /// </summary>
        public static readonly ImmutableOrderedDictionary<TKey, TValue> Empty =
            new ImmutableOrderedDictionary<TKey, TValue>(
                ImmutableDictionary<TKey, TValue>.Empty,
                ImmutableList<TKey>.Empty);

        private ImmutableDictionary<TKey, TValue> innerDictionary;
        private ImmutableList<TKey> orderedKeyList;

        /// <summary>
        /// Gets the value for a particular key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        public TValue this[TKey key] => innerDictionary[key];

        /// <summary>
        /// Gets a sequence of all keys in this dictionary.
        /// </summary>
        public IEnumerable<TKey> Keys => orderedKeyList;

        /// <summary>
        /// Gets a sequence of all values in this dictionary.
        /// </summary>
        public IEnumerable<TValue> Values => orderedKeyList.Select(GetValue);

        /// <summary>
        /// Gets the number of elements in this dictionary.
        /// </summary>
        public int Count => orderedKeyList.Count;

        private TValue GetValue(TKey key)
        {
            return innerDictionary[key];
        }

        private KeyValuePair<TKey, TValue> GetKeyValuePair(TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, innerDictionary[key]);
        }

        /// <summary>
        /// Adds a key-value pair to this ordered dictionary.
        /// </summary>
        /// <param name="key">The key to add to the dictionary.</param>
        /// <param name="value">The value to associate with <paramref name="key"/>.</param>
        /// <returns>A new dictionary that includes the key-value pair.</returns>
        public ImmutableOrderedDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            return new ImmutableOrderedDictionary<TKey, TValue>(
                innerDictionary.Add(key, value),
                orderedKeyList.Add(key));
        }

        /// <summary>
        /// Adds a sequence of key-value pairs to this ordered dictionary.
        /// </summary>
        /// <param name="pairs">The key-value pairs to add to the dictionary.</param>
        /// <returns>A new dictionary that includes the key-value pairs.</returns>
        public ImmutableOrderedDictionary<TKey, TValue> AddRange(
            IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return new ImmutableOrderedDictionary<TKey, TValue>(
                innerDictionary.AddRange(pairs),
                orderedKeyList.AddRange(pairs.Select(p => p.Key)));
        }

        /// <summary>
        /// Creates an immutable ordered dictionary that does not contain
        /// any elements.
        /// </summary>
        /// <returns>A new immutable ordered dictionary.</returns>
        public ImmutableOrderedDictionary<TKey, TValue> Clear()
        {
            return Empty;
        }

        /// <summary>
        /// Tests if this dictionary contains a particular key-value pair.
        /// </summary>
        /// <param name="pair">The key-value pair to look for.</param>
        /// <returns>
        /// <c>true</c> if this dictionary contains the key-value pair;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return innerDictionary.Contains(pair);
        }

        /// <summary>
        /// Tests if this dictionary contains a particular key.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>
        /// <c>true</c> if this dictionary contains the key;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            return innerDictionary.ContainsKey(key);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return orderedKeyList.Select(GetKeyValuePair).GetEnumerator();
        }

        /// <summary>
        /// Removes a key-value pair from this immutable ordered dictionary.
        /// </summary>
        /// <param name="key">The key of the key-value pair to remove.</param>
        /// <returns>
        /// An immutable ordered dictionary that does not contain the <paramref name="key"/>.
        /// </returns>
        public ImmutableOrderedDictionary<TKey, TValue> Remove(TKey key)
        {
            return new ImmutableOrderedDictionary<TKey, TValue>(
                innerDictionary.Remove(key),
                orderedKeyList.Remove(key));
        }

        /// <summary>
        /// Removes a sequence of key-value pairs from this immutable ordered dictionary.
        /// </summary>
        /// <param name="keys">The keys of the key-value pairs to remove.</param>
        /// <returns>
        /// An immutable ordered dictionary that does not contain <paramref name="keys"/>.
        /// </returns>
        public ImmutableOrderedDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            return new ImmutableOrderedDictionary<TKey, TValue>(
                innerDictionary.RemoveRange(keys),
                orderedKeyList.RemoveRange(keys));
        }

        /// <summary>
        /// Assigns a value to a particular key.
        /// </summary>
        /// <param name="key">The key to assign a value to.</param>
        /// <param name="value">The value to assign to the key.</param>
        /// <returns>
        /// An immutable ordered dictionary that associates <paramref name="key"/>
        /// with <paramref name="value"/>.
        /// </returns>
        public ImmutableOrderedDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            return new ImmutableOrderedDictionary<TKey, TValue>(
                innerDictionary.SetItem(key, value),
                innerDictionary.ContainsKey(key)
                    ? orderedKeyList
                    : orderedKeyList.Add(key));
        }

        /// <summary>
        /// Assigns values to keys.
        /// </summary>
        /// <param name="items">The key-value pairs to add or update.</param>
        /// <returns>
        /// An immutable ordered dictionary that contains <paramref name="items"/>
        /// as key-value pairs.
        /// </returns>
        public ImmutableOrderedDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            var oldDict = innerDictionary;
            return new ImmutableOrderedDictionary<TKey, TValue>(
                innerDictionary.SetItems(items),
                orderedKeyList.AddRange(
                    items
                        .Select(p => p.Key)
                        .Where(k => !oldDict.ContainsKey(k))));
        }

        /// <inheritdoc/>
        public bool TryGetKey(TKey equalKey, out TKey actualKey)
        {
            return innerDictionary.TryGetKey(equalKey, out actualKey);
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return innerDictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Creates a mutable version of this immutable ordered dictionary.
        /// </summary>
        /// <returns>An ordered dictionary builder.</returns>
        public Builder ToBuilder()
        {
            return new Builder(
                innerDictionary.ToBuilder(),
                orderedKeyList.ToBuilder());
        }

        /// <summary>
        /// A mutable wrapper around an immutable ordered dictionary.
        /// </summary>
        public sealed class Builder :
            IEnumerable<KeyValuePair<TKey, TValue>>,
            IReadOnlyDictionary<TKey, TValue>,
            IReadOnlyCollection<KeyValuePair<TKey, TValue>>
        {
            internal Builder(
                ImmutableDictionary<TKey, TValue>.Builder innerDictionary,
                ImmutableList<TKey>.Builder orderedKeyList)
            {
                this.innerDictionary = innerDictionary;
                this.orderedKeyList = orderedKeyList;
            }

            private ImmutableDictionary<TKey, TValue>.Builder innerDictionary;
            private ImmutableList<TKey>.Builder orderedKeyList;

            /// <summary>
            /// Gets or sets the value associated with a particular key.
            /// </summary>
            /// <param name="key">The key to access.</param>
            /// <returns>The value associated with <paramref name="key"/>.</returns>
            public TValue this[TKey key]
            {
                get
                {
                    return innerDictionary[key];
                }
                set
                {
                    if (!innerDictionary.ContainsKey(key))
                    {
                        orderedKeyList.Add(key);
                    }
                    innerDictionary[key] = value;
                }
            }

            /// <summary>
            /// Gets a sequence of all keys in this dictionary.
            /// </summary>
            public IEnumerable<TKey> Keys => orderedKeyList;

            /// <summary>
            /// Gets a sequence of all values in this dictionary.
            /// </summary>
            public IEnumerable<TValue> Values => Keys.Select(GetValue);

            /// <summary>
            /// Gets the number of key-value pairs in this dictionary.
            /// </summary>
            public int Count => orderedKeyList.Count;

            /// <summary>
            /// Gets the value comparer for this dictionary.
            /// </summary>
            public IEqualityComparer<TValue> ValueComparer => innerDictionary.ValueComparer;

            /// <summary>
            /// Gets the value comparer for this dictionary.
            /// </summary>
            public IEqualityComparer<TKey> KeyComparer => innerDictionary.KeyComparer;

            private TValue GetValue(TKey key)
            {
                return innerDictionary[key];
            }

            private KeyValuePair<TKey, TValue> GetKeyValuePair(TKey key)
            {
                return new KeyValuePair<TKey, TValue>(key, innerDictionary[key]);
            }

            /// <summary>
            /// Adds a key-value pair to this dictionary.
            /// </summary>
            /// <param name="item">The key-value pair to add.</param>
            public void Add(KeyValuePair<TKey, TValue> item)
            {
                innerDictionary.Add(item);
                orderedKeyList.Add(item.Key);
            }

            /// <summary>
            /// Adds a key-value pair to this dictionary.
            /// </summary>
            /// <param name="key">The key to add.</param>
            /// <param name="value">
            /// The value to associate with <paramref name="key"/>.
            /// </param>
            public void Add(TKey key, TValue value)
            {
                innerDictionary.Add(key, value);
                orderedKeyList.Add(key);
            }

            /// <summary>
            /// Adds a sequence of key-value pairs to this dictionary.
            /// </summary>
            /// <param name="items">The key-value pairs to add.</param>
            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
            {
                innerDictionary.AddRange(items);
                orderedKeyList.AddRange(items.Select(p => p.Key));
            }

            /// <summary>
            /// Removes all key-value pairs from this dictionary.
            /// </summary>
            public void Clear()
            {
                innerDictionary.Clear();
                orderedKeyList.Clear();
            }

            /// <summary>
            /// Tests if this dictionary contains a key-value pair.
            /// </summary>
            /// <param name="item">The key-value pair to look for.</param>
            /// <returns>
            /// <c>true</c> if this dictionary contains <paramref name="item"/>.
            /// </returns>
            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return innerDictionary.Contains(item);
            }

            /// <summary>
            /// Tests if this dictionary contains a particular key.
            /// </summary>
            /// <param name="key">The key to look for.</param>
            /// <returns>
            /// <c>true</c> if this dictionary contains <paramref name="key"/>.
            /// </returns>
            public bool ContainsKey(TKey key)
            {
                return innerDictionary.ContainsKey(key);
            }

            /// <summary>
            /// Tests if this dictionary contains a particular value.
            /// </summary>
            /// <param name="value">The value to look for.</param>
            /// <returns>
            /// <c>true</c> if this dictionary contains <paramref name="value"/>.
            /// </returns>
            public bool ContainsValue(TValue value)
            {
                return innerDictionary.ContainsValue(value);
            }

            /// <inheritdoc/>
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return Keys.Select(GetKeyValuePair).GetEnumerator();
            }

            /// <summary>
            /// Gets the value associated with a particular key or a
            /// default value if this dictionary does not contain the key.
            /// </summary>
            /// <param name="key">The key to look for.</param>
            /// <param name="defaultValue">
            /// The value to return if this dictionary does not contain
            /// <paramref name="key"/>.
            /// </param>
            /// <returns>
            /// The value associated with <paramref name="key"/> if it exists;
            /// otherwise, <paramref name="defaultValue"/>.
            /// </returns>
            public TValue GetValueOrDefault(TKey key, TValue defaultValue)
            {
                return innerDictionary.GetValueOrDefault(key, defaultValue);
            }

            /// <summary>
            /// Gets the value associated with a particular key or a
            /// default value if this dictionary does not contain the key.
            /// </summary>
            /// <param name="key">The key to look for.</param>
            /// <returns>
            /// The value associated with <paramref name="key"/> if it exists;
            /// otherwise, a default value.
            /// </returns>
            public TValue GetValueOrDefault(TKey key)
            {
                return innerDictionary.GetValueOrDefault(key);
            }

            /// <summary>
            /// Removes a key from this dictionary.
            /// </summary>
            /// <param name="key">The key to remove.</param>
            /// <returns>
            /// <c>true</c> if the key was removed;
            /// <c>false</c> if this dictionary didn't contain the
            /// key in the first place.
            /// </returns>
            public bool Remove(TKey key)
            {
                if (innerDictionary.Remove(key))
                {
                    orderedKeyList.Remove(key);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Removes a key-value pair from this dictionary.
            /// </summary>
            /// <param name="item">The key-value pair to remove.</param>
            /// <returns>
            /// <c>true</c> if the key-value pair was removed;
            /// <c>false</c> if this dictionary didn't contain the
            /// key-value pair in the first place.
            /// </returns>
            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                if (innerDictionary.Remove(item))
                {
                    orderedKeyList.Remove(item.Key);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Removes a range of keys from this ordered dictionary.
            /// </summary>
            /// <param name="keys">The keys to remove.</param>
            public void RemoveRange(IEnumerable<TKey> keys)
            {
                innerDictionary.RemoveRange(keys);

                // TODO: optimize this so it's `O(n)`.
                foreach (var k in keys)
                {
                    orderedKeyList.Remove(k);
                }
            }

            /// <summary>
            /// Tries to find a key in this dictionary.
            /// </summary>
            /// <param name="equalKey">
            /// A key that is equal to the key to look for.
            /// </param>
            /// <param name="actualKey">
            /// The key that is stored in the dictionary.
            /// </param>
            /// <returns>
            /// <c>true</c> if this dictionary contains a key
            /// equal to <paramref name="equalKey"/>; otherwise,
            /// <c>false</c>.
            /// </returns>
            public bool TryGetKey(TKey equalKey, out TKey actualKey)
            {
                return innerDictionary.TryGetKey(equalKey, out actualKey);
            }

            /// <inheritdoc/>
            public bool TryGetValue(TKey key, out TValue value)
            {
                return innerDictionary.TryGetValue(key, out value);
            }

            /// <summary>
            /// Creates an immutable ordered dictionary from this mutable builder.
            /// </summary>
            /// <returns>An immutable ordered dictionary.</returns>
            public ImmutableOrderedDictionary<TKey, TValue> ToImmutable()
            {
                return new ImmutableOrderedDictionary<TKey, TValue>(
                    innerDictionary.ToImmutable(),
                    orderedKeyList.ToImmutable());
            }

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear()
        {
            return Clear();
        }

        /// <inheritdoc/>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            return Add(key, value);
        }

        /// <inheritdoc/>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return AddRange(pairs);
        }

        /// <inheritdoc/>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value)
        {
            return SetItem(key, value);
        }

        /// <inheritdoc/>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            return SetItems(items);
        }


        /// <inheritdoc/>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys)
        {
            return RemoveRange(keys);
        }

        /// <inheritdoc/>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key)
        {
            return Remove(key);
        }
    }
}
