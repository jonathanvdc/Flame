using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    public sealed class WeakCache<TKey, TValue> : Cache<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        // WeakCache<TKey, TValue> is implemented as a dictionary:
        // it contains an array of buckets and each bucket contains
        // a list of (hash code, weak key, weak value) triples.

        public WeakCache(IEqualityComparer<TKey> keyComparer)
        {
            this.keyComparer = keyComparer;
            this.buckets = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>[primes[0]];
        }

        private ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>[] buckets;
        private IEqualityComparer<TKey> keyComparer;

        private static readonly int[] primes = new int[]
        {
            31, 97, 389, 1543, 6151, 24593,
            98317, 393241, 1572869, 6291469,
            25165843, 100663319, 402653189, 1610612741
        };

        private int TruncateHashCode(int hashCode)
        {
            return (hashCode & int.MaxValue) % buckets.Length;
        }

        private void OverwriteValue(
            int keyHashCode, TKey key, TValue value,
            ref ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>> bucket)
        {
            bool inserted = false;
            for (int i = 0; i < bucket.Count;)
            {
                var entry = bucket[i];
                TKey entryKey;
                TValue entryValue;
                if (!entry.Key.TryGetTarget(out entryKey)
                    || !entry.Value.TryGetTarget(out entryValue))
                {
                    // This entry has expired. Remove it and continue to the
                    // next iteration without incrementing i.
                    bucket.RemoveAt(i);
                    continue;
                }
                if (keyHashCode == entry.KeyHashCode
                    && keyComparer.Equals(key, entryKey))
                {
                    inserted = true;
                    bucket[i] =
                        new HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>(
                            keyHashCode,
                            entry.Key,
                            new WeakReference<TValue>(value));
                }
                i++;
            }

            if (!inserted)
            {
                bucket.Add(
                    new HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>(
                        keyHashCode,
                        new WeakReference<TKey>(key),
                        new WeakReference<TValue>(value)));
            }
        }

        private bool TryFindValue(
            int keyHashCode, TKey key, out TValue value,
            ref ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>> bucket)
        {
            bool found = false;
            value = default(TValue);
            for (int i = 0; i < bucket.Count;)
            {
                var entry = bucket[i];
                TKey entryKey;
                TValue entryValue;
                if (!entry.Key.TryGetTarget(out entryKey)
                    || !entry.Value.TryGetTarget(out entryValue))
                {
                    // This entry has expired. Remove it and continue to the
                    // next iteration without incrementing i.
                    bucket.RemoveAt(i);
                    continue;
                }
                if (keyHashCode == entry.KeyHashCode
                    && keyComparer.Equals(key, entryKey))
                {
                    found = true;
                    value = entryValue;
                }
                i++;
            }
            return found;
        }

        /// <inheritdoc/>
        public sealed override void Insert(TKey key, TValue value)
        {
            var hashCode = keyComparer.GetHashCode(key);

            int index = TruncateHashCode(hashCode);
            var bucket = buckets[index];
            if (!bucket.IsInitialized)
            {
                bucket = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>(4);
            }

            OverwriteValue(hashCode, key, value, ref bucket);
            buckets[index] = bucket;
        }

        /// <inheritdoc/>
        public sealed override bool TryGet(TKey key, out TValue value)
        {
            var hashCode = keyComparer.GetHashCode(key);
            var bucket = buckets[TruncateHashCode(hashCode)];
            return TryFindValue(hashCode, key, out value, ref bucket);
        }
    }
}