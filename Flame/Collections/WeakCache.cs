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
        //
        // These buckets are iterated through to find the values
        // associated with keys. Triples whose keys and/or values
        // are garbage-collected are removed when buckets are iterated
        // through. Additionally, after `2 * bucket count` accesses, the
        // entire cache is traversed for dead triples.
        //
        // When the number of non-empty buckets exceeds
        // `3/4 * bucket count`, the buckets array is bumped to the
        // next prime number in `primes` and triples are moved into
        // new buckets.

        public WeakCache(IEqualityComparer<TKey> keyComparer)
        {
            this.keyComparer = keyComparer;
            this.buckets = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>[primes[0]];
            this.initializedBucketCount = 0;
            this.accessCount = 0;
        }

        private ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>[] buckets;
        private IEqualityComparer<TKey> keyComparer;
        private int initializedBucketCount;
        private int accessCount;

        private static readonly int[] primes = new int[]
        {
            31, 97, 389, 1543, 6151, 24593,
            98317, 393241, 1572869, 6291469,
            25165843, 100663319, 402653189, 1610612741
        };

        private const int initialBucketSize = 4;

        private int TruncateHashCode(int hashCode)
        {
            return TruncateHashCode(hashCode, buckets.Length);
        }

        private static int TruncateHashCode(int hashCode, int bucketCount)
        {
            return (hashCode & int.MaxValue) % bucketCount;
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

        private bool TryGetNextPrime(out int nextPrime)
        {
            int numBuckets = buckets.Length;
            for (int i = 0; i < primes.Length; i++)
            {
                if (primes[i] > numBuckets)
                {
                    nextPrime = primes[i];
                    return true;
                }
            }
            nextPrime = 0;
            return false;
        }

        private void ResizeTo(int bucketCount)
        {
            var newBuckets = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>[
                bucketCount];

            initializedBucketCount = 0;
            for (int i = 0; i < buckets.Length; i++)
            {
                var oldBucket = buckets[i];
                for (int j = 0; j < oldBucket.Count; j++)
                {
                    var entry = oldBucket[j];
                    TKey entryKey;
                    TValue entryValue;
                    if (entry.Key.TryGetTarget(out entryKey)
                        && entry.Value.TryGetTarget(out entryValue))
                    {
                        // This entry is still alive. Add it to the right
                        // bucket in the new bucket array.
                        var newBucketIndex = TruncateHashCode(entry.KeyHashCode, bucketCount);
                        var newBucket = newBuckets[newBucketIndex];
                        if (!newBucket.IsInitialized)
                        {
                            newBucket = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>(
                                initialBucketSize);
                            initializedBucketCount++;
                        }
                        newBucket.Add(entry);
                        newBuckets[newBucketIndex] = newBucket;
                    }
                }
            }
            buckets = newBuckets;
            accessCount = 0;
        }

        private void RegisterAccess()
        {
            accessCount++;

            int nextPrime;
            if (initializedBucketCount > 3 * buckets.Length / 4
                && TryGetNextPrime(out nextPrime))
            {
                ResizeTo(nextPrime);
            }
            else if (accessCount > 2 * buckets.Length)
            {
                ResizeTo(buckets.Length);
            }
        }

        /// <inheritdoc/>
        public sealed override void Insert(TKey key, TValue value)
        {
            RegisterAccess();

            var hashCode = keyComparer.GetHashCode(key);

            int index = TruncateHashCode(hashCode);
            var bucket = buckets[index];
            if (!bucket.IsInitialized)
            {
                bucket = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>(
                    initialBucketSize);
                initializedBucketCount++;
            }

            OverwriteValue(hashCode, key, value, ref bucket);
            buckets[index] = bucket;
        }

        /// <inheritdoc/>
        public sealed override bool TryGet(TKey key, out TValue value)
        {
            RegisterAccess();

            var hashCode = keyComparer.GetHashCode(key);
            var bucket = buckets[TruncateHashCode(hashCode)];
            if (bucket.IsInitialized)
            {
                return TryFindValue(hashCode, key, out value, ref bucket);
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }
    }
}