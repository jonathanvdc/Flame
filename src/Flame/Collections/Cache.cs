using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// A base class for cache data structures.
    /// </summary>
    public abstract class Cache<TKey, TValue>
    {
        /// <summary>
        /// Inserts a new key-value pair into this cache,
        /// or overwrites the value for an existing key.
        /// </summary>
        /// <param name="key">The cached value's key.</param>
        /// <param name="value">The value to cache.</param>
        public abstract void Insert(TKey key, TValue value);

        /// <summary>
        /// Tries to query the cache for the value with a
        /// particular key.
        /// </summary>
        /// <param name="key">The key of the value to query.</param>
        /// <param name="value">A cached value, if any.</param>
        /// <returns>
        /// <c>true</c> if a value for the given key was
        /// found in the cache; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryGet(TKey key, out TValue value);

        /// <summary>
        /// Queries the cache for the value with a particular key.
        /// If that value cannot be found, the key is recomputed.
        /// </summary>
        /// <param name="key">The key of the value to query.</param>
        /// <param name="createValue">
        /// A callback that creates the value for the key,
        /// in case the key was not in the cache.
        /// </param>
        /// <returns>The value for the key.</returns>
        public virtual TValue Get(TKey key, Func<TKey, TValue> createValue)
        {
            TValue result;
            if (!TryGet(key, out result))
            {
                result = createValue(key);
                Insert(key, result);
            }
            return result;
        }

        /// <summary>
        /// Tests if this cache contains a particular key.
        /// </summary>
        /// <returns><c>true</c> if the key is in the cache; <c>false</c> otherwise.</returns>
        /// <param name="key">The key to look for.</param>
        public virtual bool ContainsKey(TKey key)
        {
            TValue value;
            return TryGet(key, out value);
        }
    }

    /// <summary>
    /// A cache implementation that uses the least recently used
    /// (LRU) policy to evict stale key-value pairs.
    /// </summary>
    public sealed class LruCache<TKey, TValue> : Cache<TKey, TValue>
    {
        /// <summary>
        /// Creates an LRU cache with a particular capacity.
        /// </summary>
        /// <param name="capacity">
        /// The maximal number of key-value pairs in the LRU cache.
        /// </param>
        public LruCache(int capacity)
        {
            this.Capacity = capacity;
            this.cache =
                new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            this.evictionList = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        /// <summary>
        /// Creates an LRU cache with a particular capacity and a
        /// key equality comparer.
        /// </summary>
        /// <param name="capacity">
        /// The maximal number of key-value pairs in the LRU cache.
        /// </param>
        /// <param name="comparer">
        /// An equality comparer for keys.
        /// </param>
        public LruCache(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.Capacity = capacity;
            this.cache =
                new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(comparer);
            this.evictionList = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        /// <summary>
        /// Gets the LRU cache's capacity.
        /// </summary>
        /// <returns>
        /// The maximal number of key-value pairs in the LRU cache.
        /// </returns>
        public int Capacity { get; private set; }

        private Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> cache;
        private LinkedList<KeyValuePair<TKey, TValue>> evictionList;

        /// <inheritdoc/>
        public override void Insert(TKey key, TValue value)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (cache.TryGetValue(key, out node))
            {
                RegisterUse(node);
                node.Value = new KeyValuePair<TKey, TValue>(key, value);
            }
            else
            {
                InsertNew(key, value);
            }
        }

        /// <inheritdoc/>
        public override bool TryGet(TKey key, out TValue value)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (cache.TryGetValue(key, out node))
            {
                RegisterUse(node);
                value = node.Value.Value;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        /// <inheritdoc/>
        public override TValue Get(TKey key, Func<TKey, TValue> createValue)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node;
            if (cache.TryGetValue(key, out node))
            {
                RegisterUse(node);
                return node.Value.Value;
            }
            else
            {
                var value = createValue(key);
                InsertNew(key, value);
                return value;
            }
        }

        private void RegisterUse(LinkedListNode<KeyValuePair<TKey, TValue>> node)
        {
            // Move the cached value node to the front of the
            // linked list.
            evictionList.Remove(node);
            evictionList.AddFirst(node);
        }

        private void InsertNew(TKey key, TValue value)
        {
            // Create a new node, add it to the eviction list and
            // the cache.
            var node = new LinkedListNode<KeyValuePair<TKey, TValue>>(
                new KeyValuePair<TKey, TValue>(key, value));

            evictionList.AddFirst(node);
            cache[key] = node;

            if (evictionList.Count > Capacity)
            {
                // Remove the last node from the eviction list as
                // well as the cache if we ran out of space.
                var evictedNode = evictionList.Last;
                evictionList.RemoveLast();
                cache.Remove(evictedNode.Value.Key);
            }
        }
    }
}