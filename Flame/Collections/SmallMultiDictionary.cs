using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Flame.Collections
{
    /// <summary>
    /// A key-value pair that includes the hash code of the key.
    /// </summary>
    internal struct HashedKeyValuePair<TKey, TValue>
    {
        /// <summary>
        /// Creates a hashed key-value pair from the key's hash code,
        /// a key and a value.
        /// </summary>
        /// <param name="keyHashCode">The key's hash code.</param>
        /// <param name="key">A key.</param>
        /// <param name="value">A value.</param>
        public HashedKeyValuePair(int keyHashCode, TKey key, TValue value)
        {
            this.KeyHashCode = keyHashCode;
            this.Key = key;
            this.Value = value;
        }

        /// <summary>
        /// Gets the cached hash code of the key.
        /// </summary>
        /// <returns>The key's hash code.</returns>
        public int KeyHashCode { get; private set; }

        /// <summary>
        /// Gets the key in this key-value pair.
        /// </summary>
        /// <returns>The key.</returns>
        public TKey Key { get; private set; }

        /// <summary>
        /// Gets the value in this key-value pair.
        /// </summary>
        /// <returns>The value.</returns>
        public TValue Value { get; private set; }
    }

    /// <summary>
    /// A small, memory-friendly and cache-friendly dictionary
    /// with O(n) asymptotic complexity for all operations.
    /// </summary>
    public sealed class SmallMultiDictionary<TKey, TValue>
    {
        /// <summary>
        /// Creates a small multi-dictionary.
        /// </summary>
        public SmallMultiDictionary()
        {
            this.keyEq = EqualityComparer<TKey>.Default;
            this.pairs = new ValueList<HashedKeyValuePair<TKey, TValue>>(6);
        }

        /// <summary>
        /// Creates a small multi-dictionary from an initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The dictionary's initial capacity.</param>
        public SmallMultiDictionary(int initialCapacity)
        {
            this.keyEq = EqualityComparer<TKey>.Default;
            this.pairs = new ValueList<HashedKeyValuePair<TKey, TValue>>(initialCapacity);
        }

        /// <summary>
        /// Creates a small multi-dictionary by copying the contents of
        /// another small multi-dictionary.
        /// </summary>
        /// <param name="other">The other multi-dictionary.</param>
        public SmallMultiDictionary(SmallMultiDictionary<TKey, TValue> other)
        {
            this.pairs = new ValueList<HashedKeyValuePair<TKey, TValue>>(other.pairs);
            this.keyEq = other.keyEq;
        }

        private EqualityComparer<TKey> keyEq;
        internal ValueList<HashedKeyValuePair<TKey, TValue>> pairs;

        /// <summary>
        /// Gets the number of elements in the multi-dictionary.
        /// </summary>
        /// <returns>The number of elements in the multi-dictionary.</returns>
        public int Count { get { return pairs.Count; } }

        /// <summary>
        /// Reserves the given capacity in this small multi dictionary.
        /// </summary>
        public void Reserve(int MinimalCapacity)
        {
            pairs.Reserve(MinimalCapacity);
        }

        /// <summary>
        /// Inserts an item into this dictionary.
        /// </summary>
        public void Add(TKey Key, TValue Value)
        {
            pairs.Add(new HashedKeyValuePair<TKey, TValue>(
                keyEq.GetHashCode(Key), Key, Value));
        }

        /// <summary>
        /// Removes all key-value pairs with the given key from this dictionary.
        /// </summary>
        public bool Remove(TKey Key)
        {
            int hashCode = keyEq.GetHashCode(Key);
            var items = pairs.Items;

            bool removedAny = false;
            int i = 0;
            while (i < pairs.Count)
            {
                if (items[i].KeyHashCode == hashCode
                    && keyEq.Equals(Key, items[i].Key))
                {
                    pairs.RemoveAt(i);
                    removedAny = true;
                }
                else
                {
                    i++;
                }
            }
            return removedAny;
        }

        /// <summary>
        /// Inserts a range of items into this dictionary.
        /// </summary>
        public void AddRange(SmallMultiDictionary<TKey, TValue> Other)
        {
            pairs.AddRange(Other.pairs);
        }

        /// <summary>
        /// Checks if this dictionary contains at least one element with
        /// the given key.
        /// </summary>
        public bool ContainsKey(TKey Key)
        {
            int hashCode = keyEq.GetHashCode(Key);
            var items = pairs.Items;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (items[i].KeyHashCode == hashCode
                    && keyEq.Equals(Key, items[i].Key))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all values in this dictionary.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                return pairs.ToArray().Select(item => item.Value);
            }
        }

        /// <summary>
        /// Gets all values with the given key.
        /// </summary>
        public IEnumerable<TValue> GetAll(TKey Key)
        {
            int hashCode = keyEq.GetHashCode(Key);
            var results = new List<TValue>();
            var items = pairs.Items;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (items[i].KeyHashCode == hashCode
                    && keyEq.Equals(Key, items[i].Key))
                {
                    results.Add(items[i].Value);
                }
            }
            return results;
        }

        /// <summary>
        /// Tries to retrieve the first value with the given key in this
        /// dictionary.
        /// </summary>
        public bool TryPeek(TKey Key, out TValue Result)
        {
            int hashCode = keyEq.GetHashCode(Key);
            var items = pairs.Items;
            for (int i = 0; i < pairs.Count; i++)
            {
                if (items[i].KeyHashCode == hashCode
                    && keyEq.Equals(Key, items[i].Key))
                {
                    Result = items[i].Value;
                    return true;
                }
            }

            Result = default(TValue);
            return false;
        }

        /// <summary>
        /// Tries to retrieve the first value with the given key in this
        /// dictionary. If no such item exists, then the default value is
        /// returned.
        /// </summary>
        public TValue PeekOrDefault(TKey Key)
        {
            TValue result;
            TryPeek(Key, out result);
            return result;
        }
    }
}
