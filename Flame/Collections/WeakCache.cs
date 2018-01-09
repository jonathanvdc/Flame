using System;
using System.Collections.Generic;
using System.Threading;

namespace Flame.Collections
{
    /// <summary>
    /// A cache that maps keys to values for as long as neither the key
    /// nor the value of a key-value pair is garbage-collected. Weak
    /// caches do not prevent keys or values from being garbage-collected.
    /// </summary>
    /// <remarks>
    /// Public instance methods of this class are thread-safe.
    /// </remarks>
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
        // through.
        //
        // To support multi-threaded access to weak caches, buckets are
        // subdivided in concurrency domains such that the ith
        // concurrency domain consists of the following bucket indices:
        //
        //     { i, i + maxConcurrency, i + 2 * maxConcurrency, ... }.
        //
        // Each concurrency domain is protected by a single lock, so
        // it is entirely possible to access, say, buckets one and two
        // simultaneously (provided that maxConcurrency >= 2).
        //
        // Concurrency domains are also responsible for keeping their
        // buckets tidy: after `2 * bucket count / maxConcurrency`
        // accesses, all buckets in the concurrency domain are traversed
        // for dead triples.
        //
        // When the number of non-empty buckets exceeds
        // `3/4 * bucket count`, the buckets array is bumped to the
        // next prime number in `primes` and triples are moved into
        // new buckets. This requires taking a global lock.

        /// <summary>
        /// Creates a weak cache that uses a particular key comparer
        /// under the hood.
        /// </summary>
        /// <param name="keyComparer">A key comparer.</param>
        public WeakCache(IEqualityComparer<TKey> keyComparer)
            : this(keyComparer, Environment.ProcessorCount)
        { }

        /// <summary>
        /// Creates a weak cache that uses a particular key comparer
        /// under the hood and can be accessed by up to maxConcurrency
        /// threads simultaneously (under ideal circumstances).
        /// </summary>
        /// <param name="keyComparer">A key comparer.</param>
        /// <param name="maxConcurrency">
        /// The maximum number of threads that can access the weak cache
        /// simultaneously under optimal circumstances.
        /// </param>
        public WeakCache(IEqualityComparer<TKey> keyComparer, int maxConcurrency)
        {
            this.keyComparer = keyComparer;
            this.buckets = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>[primes[0]];
            this.initializedBucketCount = 0;
            this.accessCounters = new int[maxConcurrency];
            this.domainLocks = new object[maxConcurrency];
            this.resizeLock = new ReaderWriterLockSlim();

            for (int i = 0; i < maxConcurrency; i++)
            {
                this.accessCounters[i] = 0;
                this.domainLocks[i] = new object();
            }
        }

        private ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>[] buckets;
        private IEqualityComparer<TKey> keyComparer;
        private int initializedBucketCount;
        private int[] accessCounters;
        private object[] domainLocks;
        private ReaderWriterLockSlim resizeLock;

        /// <summary>
        /// Gets the maximum number of concurrent accesses to this cache.
        /// </summary>
        private int MaxConcurrency => domainLocks.Length;

        private static readonly int[] primes = new int[]
        {
            31, 97, 389, 1543, 6151, 24593,
            98317, 393241, 1572869, 6291469,
            25165843, 100663319, 402653189, 1610612741
        };

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
            for (int i = bucket.Count - 1; i >= 0; i--)
            {
                var entry = bucket[i];
                TKey entryKey;
                TValue entryValue;
                if (!entry.Key.TryGetTarget(out entryKey)
                    || !entry.Value.TryGetTarget(out entryValue))
                {
                    // This entry has expired. Remove it and continue to the
                    // next iteration.
                    bucket.RemoveAt(i);
                }
                else if (keyHashCode == entry.KeyHashCode
                    && keyComparer.Equals(key, entryKey))
                {
                    inserted = true;
                    bucket[i] =
                        new HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>(
                            keyHashCode,
                            entry.Key,
                            new WeakReference<TValue>(value));
                }
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
            for (int i = bucket.Count - 1; i >= 0; i--)
            {
                var entry = bucket[i];
                TKey entryKey;
                TValue entryValue;
                if (!entry.Key.TryGetTarget(out entryKey)
                    || !entry.Value.TryGetTarget(out entryValue))
                {
                    // This entry has expired. Remove it and continue to the
                    // next iteration.
                    bucket.RemoveAt(i);
                }
                else if (keyHashCode == entry.KeyHashCode
                    && keyComparer.Equals(key, entryKey))
                {
                    found = true;
                    value = entryValue;
                }
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

        /// <summary>
        /// Resizes the cache to a particular number of buckets.
        /// </summary>
        /// <param name="bucketCount">
        /// The number of buckets to resize the cache to.
        /// </param>
        /// <remarks>This method is not thread-safe.</remarks>
        private void ResizeToImpl(int bucketCount)
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
                            newBucket = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>(4);
                            initializedBucketCount++;
                        }
                        newBucket.Add(entry);
                        newBuckets[newBucketIndex] = newBucket;
                    }
                }
            }

            buckets = newBuckets;
            for (int i = 0; i < accessCounters.Length; i++)
            {
                accessCounters[i] = 0;
            }
        }

        /// <summary>
        /// Resizes the cache to a particular number of buckets.
        /// </summary>
        /// <param name="bucketCount">
        /// The number of buckets to resize the cache to.
        /// </param>
        /// <remarks>
        /// This method is thread-safe, but must be called without the
        /// resize lock acquired.
        /// </remarks>
        private void ResizeTo(int bucketCount)
        {
            try
            {
                resizeLock.EnterWriteLock();

                if (buckets.Length < bucketCount)
                {
                    // Make sure we check if the number of buckets is not
                    // already greater than or equal to the desired
                    // number of buckets: another thread could have
                    // resized the cache while we were waiting for the
                    // resize lock.
                    ResizeToImpl(bucketCount);
                }
            }
            finally
            {
                resizeLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Cleans up all buckets allocated to a particular concurrency
        /// domain.
        /// </summary>
        /// <param name="concurrencyDomain">
        /// The concurrency domain whose buckets are to be cleaned.
        /// </param>
        /// <remarks>
        /// This method is thread-safe within a single concurrency domain.
        /// </remarks>
        private void Cleanup(int concurrencyDomain)
        {
            for (int i = concurrencyDomain; i < buckets.Length; i += MaxConcurrency)
            {
                var bucket = buckets[i];
                for (int j = bucket.Count - 1; j >= 0; j--)
                {
                    var entry = bucket[j];
                    TKey entryKey;
                    TValue entryValue;
                    if (!entry.Key.TryGetTarget(out entryKey)
                        || !entry.Value.TryGetTarget(out entryValue))
                    {
                        // This entry is dead. Remove it from the bucket.
                        bucket.RemoveAt(j);
                    }
                }
                buckets[i] = bucket;
            }
        }

        /// <summary>
        /// Registers an access to a concurrency domain.
        /// </summary>
        /// <param name="concurrencyDomain">
        /// The concurrency domain to access.
        /// </param>
        /// <param name="resizeSize">
        /// The number of buckets to resize the cache to.
        /// </param>
        /// <returns>
        /// <c>true</c> if this cache should be resized; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is thread-safe within a single concurrency domain.
        /// </remarks>
        private bool RegisterAccess(int concurrencyDomain, out int resizeSize)
        {
            accessCounters[concurrencyDomain]++;

            if (initializedBucketCount > 3 * buckets.Length / 4
                && TryGetNextPrime(out resizeSize))
            {
                return true;
            }
            else if (accessCounters[concurrencyDomain] > 2 * buckets.Length)
            {
                resizeSize = 0;
                Cleanup(concurrencyDomain);
                return false;
            }
            else
            {
                resizeSize = 0;
                return false;
            }
        }

        /// <summary>
        /// Gets the concurrency domain to which the bucket with
        /// a specific index has been assigned.
        /// </summary>
        /// <param name="bucketIndex">
        /// The index of the bucket assigned to the concurrency
        /// domain to discover.
        /// </param>
        /// <returns>
        /// The concurrency domain to which the bucket is assigned.
        /// </returns>
        private int GetConcurrencyDomain(int bucketIndex)
        {
            return bucketIndex % MaxConcurrency;
        }

        /// <inheritdoc/>
        public sealed override void Insert(TKey key, TValue value)
        {
            var hashCode = keyComparer.GetHashCode(key);
            int bucketIndex = TruncateHashCode(hashCode);
            int domain = GetConcurrencyDomain(bucketIndex);

            bool mustResize;
            int resizeSize;

            try
            {
                resizeLock.EnterReadLock();

                lock (domainLocks[domain])
                {
                    mustResize = RegisterAccess(domain, out resizeSize);
                    var bucket = buckets[bucketIndex];
                    if (!bucket.IsInitialized)
                    {
                        bucket = new ValueList<HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>>(4);
                        initializedBucketCount++;
                    }

                    OverwriteValue(hashCode, key, value, ref bucket);
                    buckets[bucketIndex] = bucket;
                }
            }
            finally
            {
                resizeLock.ExitReadLock();
            }

            if (mustResize)
            {
                ResizeTo(resizeSize);
            }
        }

        /// <inheritdoc/>
        public sealed override bool TryGet(TKey key, out TValue value)
        {
            var hashCode = keyComparer.GetHashCode(key);
            int bucketIndex = TruncateHashCode(hashCode);
            int domain = GetConcurrencyDomain(bucketIndex);

            bool mustResize;
            int resizeSize;
            bool result;

            try
            {
                resizeLock.EnterReadLock();

                lock (domainLocks[domain])
                {
                    mustResize = RegisterAccess(domain, out resizeSize);
                    var bucket = buckets[bucketIndex];
                    if (bucket.IsInitialized)
                    {
                        result = TryFindValue(hashCode, key, out value, ref bucket);
                    }
                    else
                    {
                        value = default(TValue);
                        result = false;
                    }
                }
            }
            finally
            {
                resizeLock.ExitReadLock();
            }

            if (mustResize)
            {
                ResizeTo(resizeSize);
            }

            return result;
        }
    }
}