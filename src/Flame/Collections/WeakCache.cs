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
        /// Creates a weak cache.
        /// </summary>
        public WeakCache()
            : this(EqualityComparer<TKey>.Default)
        { }

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
            this.buckets = new WeakCacheBucket<TKey, TValue>[primes[0]];
            this.initializedBucketCount = 0;
            this.accessCounters = new int[maxConcurrency];
            this.domainLocks = new object[maxConcurrency];
            this.resizeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

            for (int i = 0; i < maxConcurrency; i++)
            {
                this.accessCounters[i] = 0;
                this.domainLocks[i] = new object();
            }
        }

        private WeakCacheBucket<TKey, TValue>[] buckets;
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
            var newBuckets = new WeakCacheBucket<TKey, TValue>[
                bucketCount];

            initializedBucketCount = 0;
            for (int i = 0; i < buckets.Length; i++)
            {
                var oldBucket = buckets[i];
                while (!oldBucket.IsEmpty)
                {
                    var kvPair = oldBucket.keyValuePair;
                    TKey entryKey;
                    TValue entryValue;
                    if (kvPair.Key.TryGetTarget(out entryKey)
                        && kvPair.Value.TryGetTarget(out entryValue))
                    {
                        // This entry is still alive. Add it to the right
                        // bucket in the new bucket array.
                        var newBucketIndex = TruncateHashCode(kvPair.KeyHashCode, bucketCount);
                        var newBucket = newBuckets[newBucketIndex];
                        if (newBucket.IsEmpty)
                        {
                            initializedBucketCount++;
                        }
                        newBucket.Add(kvPair);
                        newBuckets[newBucketIndex] = newBucket;
                    }

                    if (oldBucket.spilloverList == null)
                    {
                        break;
                    }
                    else
                    {
                        oldBucket = oldBucket.spilloverList.contents;
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
                ContractHelpers.Assert(GetConcurrencyDomain(i) == concurrencyDomain);

                var bucket = buckets[i];
                bool wasEmpty = bucket.IsEmpty;
                bucket.Cleanup();
                if (!wasEmpty && bucket.IsEmpty)
                {
                    Interlocked.Decrement(ref initializedBucketCount);
                }
                buckets[i] = bucket;
            }
        }

        /// <summary>
        /// Explictly cleans up all outdated keys in the weak cache.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                resizeLock.EnterWriteLock();

                for (int i = 0; i < MaxConcurrency; i++)
                {
                    Cleanup(i);
                }
            }
            finally
            {
                resizeLock.ExitWriteLock();
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

        private bool AcquireBucket(int bucketIndex, out int resizeSize)
        {
            int domain = GetConcurrencyDomain(bucketIndex);
            Monitor.Enter(domainLocks[domain]);
            return RegisterAccess(domain, out resizeSize);
        }

        private void ReleaseBucket(int bucketIndex)
        {
            int domain = GetConcurrencyDomain(bucketIndex);
            Monitor.Exit(domainLocks[domain]);
        }

        /// <inheritdoc/>
        public sealed override void Insert(TKey key, TValue value)
        {
            var hashCode = keyComparer.GetHashCode(key);

            bool mustResize;
            int resizeSize;

            try
            {
                resizeLock.EnterReadLock();

                int bucketIndex = TruncateHashCode(hashCode);

                try
                {
                    mustResize = AcquireBucket(bucketIndex, out resizeSize);

                    var bucket = buckets[bucketIndex];
                    if (bucket.IsEmpty)
                    {
                        Interlocked.Increment(ref initializedBucketCount);
                    }
                    bucket.OverwriteOrAdd(keyComparer, hashCode, key, value);
                    buckets[bucketIndex] = bucket;
                }
                finally
                {
                    ReleaseBucket(bucketIndex);
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

            bool mustResize;
            int resizeSize;
            bool result;

            try
            {
                resizeLock.EnterReadLock();

                int bucketIndex = TruncateHashCode(hashCode);

                try
                {
                    mustResize = AcquireBucket(bucketIndex, out resizeSize);

                    var bucket = buckets[bucketIndex];
                    bool wasEmpty = bucket.IsEmpty;
                    result = bucket.TryFindValue(keyComparer, hashCode, key, out value);
                    if (!wasEmpty && bucket.IsEmpty)
                    {
                        Interlocked.Decrement(ref initializedBucketCount);
                    }
                    buckets[bucketIndex] = bucket;
                }
                finally
                {
                    ReleaseBucket(bucketIndex);
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

        /// <inheritdoc/>
        public override TValue Get(TKey key, Func<TKey, TValue> createValue)
        {
            TValue result;
            if (TryGet(key, out result))
            {
                return result;
            }
            else
            {
                return TryAddNew(key, createValue(key));
            }
        }

        private TValue TryAddNew(TKey key, TValue newValue)
        {
            var hashCode = keyComparer.GetHashCode(key);

            bool mustResize;
            int resizeSize;
            TValue result;

            try
            {
                resizeLock.EnterReadLock();

                int bucketIndex = TruncateHashCode(hashCode);

                try
                {
                    mustResize = AcquireBucket(bucketIndex, out resizeSize);

                    var bucket = buckets[bucketIndex];
                    if (bucket.IsEmpty)
                    {
                        Interlocked.Increment(ref initializedBucketCount);
                    }

                    if (!bucket.TryFindValue(keyComparer, hashCode, key, out result))
                    {
                        result = newValue;
                        bucket.Add(hashCode, key, result);
                        buckets[bucketIndex] = bucket;
                    }
                }
                finally
                {
                    ReleaseBucket(bucketIndex);
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

    internal struct WeakCacheBucket<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        /// <summary>
        /// Tests if this key-value bucket is empty.
        /// </summary>
        public bool IsEmpty => keyValuePair.Key == null;

        public HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>> keyValuePair;
        public WeakCacheBucketNode<TKey, TValue> spilloverList;

        /// <summary>
        /// Tries to find the value that is associated with a particular key.
        /// </summary>
        /// <param name="keyComparer">An equality comparer for keys.</param>
        /// <param name="keyHashCode">The hash code for the key.</param>
        /// <param name="key">The key to search for.</param>
        /// <param name="value">The value associated with the key.</param>
        public bool TryFindValue(
            IEqualityComparer<TKey> keyComparer,
            int keyHashCode,
            TKey key,
            out TValue value)
        {
            TKey kvPairKey;
            TValue kvPairValue;
            if (!TryIsolateFirstLiveEntry(out kvPairKey, out kvPairValue))
            {
                value = default(TValue);
                return false;
            }

            if (keyValuePair.KeyHashCode == keyHashCode
                && keyComparer.Equals(key, kvPairKey))
            {
                value = kvPairValue;
                return true;
            }
            else if (spilloverList != null)
            {
                return spilloverList.contents.TryFindValue(
                    keyComparer, keyHashCode, key, out value);
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// Adds a new key-value pair to this bucket. The key must not
        /// be in the bucket yet.
        /// </summary>
        /// <param name="keyHashCode">The key's hash code.</param>
        /// <param name="key">The key to add to this bucket.</param>
        /// <param name="value">The value to associate with the key.</param>
        public void Add(int keyHashCode, TKey key, TValue value)
        {
            Add(
                new HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>>(
                    keyHashCode,
                    new WeakReference<TKey>(key),
                    new WeakReference<TValue>(value)));
        }

        /// <summary>
        /// Adds a new key-value pair to this bucket. The key-value pair's
        /// key must not be present in the bucket yet.
        /// </summary>
        /// <param name="kvPair">The key-value pair to add.</param>
        public void Add(
            HashedKeyValuePair<WeakReference<TKey>, WeakReference<TValue>> kvPair)
        {
            if (!IsEmpty)
            {
                var newSpilloverlist = new WeakCacheBucketNode<TKey, TValue>(this);
                this.spilloverList = newSpilloverlist;
            }

            this.keyValuePair = kvPair;
        }

        /// <summary>
        /// Tries to overwrite the value for a key-value pair.
        /// </summary>
        /// <param name="keyComparer">An equality comparer for keys.</param>
        /// <param name="keyHashCode">The hash code for the key.</param>
        /// <param name="key">The key to add to the bucket.</param>
        /// <param name="value">The value to associate with the key.</param>
        private bool TryOverwrite(
            IEqualityComparer<TKey> keyComparer,
            int keyHashCode,
            TKey key,
            TValue value)
        {
            TKey kvPairKey;
            TValue kvPairValue;
            if (!TryIsolateFirstLiveEntry(out kvPairKey, out kvPairValue))
            {
                return false;
            }

            if (keyValuePair.KeyHashCode == keyHashCode
                && keyComparer.Equals(key, kvPairKey))
            {
                // Update both the value *and* the key.
                //
                // The latter needs to be updated because the
                // key is a weak reference. To leave the old key
                // in place is an invitation for the GC to kick in
                // and collect the old key, making the *new*
                // key-value pair invalid.
                keyValuePair.Key.SetTarget(key);
                keyValuePair.Value.SetTarget(value);
                return true;
            }
            else if (spilloverList != null)
            {
                // Try to overwrite the value in the spillover list.
                return spilloverList.contents.TryOverwrite(
                    keyComparer, keyHashCode, key, value);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Overwrites the value for a key-value pair or adds a
        /// new key-value pair to this bucket.
        /// </summary>
        /// <param name="keyComparer">An equality comparer for keys.</param>
        /// <param name="keyHashCode">The hash code for the key.</param>
        /// <param name="key">The key to add to the bucket.</param>
        /// <param name="value">The value to associate with the key.</param>
        public void OverwriteOrAdd(
            IEqualityComparer<TKey> keyComparer,
            int keyHashCode,
            TKey key,
            TValue value)
        {
            if (!TryOverwrite(keyComparer, keyHashCode, key, value))
            {
                Add(keyHashCode, key, value);
            }
        }

        /// <summary>
        /// Deletes the first element in this bucket.
        /// </summary>
        private void DeleteFirst()
        {
            if (spilloverList == null)
            {
                this = default(WeakCacheBucket<TKey, TValue>);
            }
            else
            {
                this = spilloverList.contents;
            }
        }

        /// <summary>
        /// Deletes all leading dead entries and returns the first
        /// live entry, if any. Then returns the first entry, if
        /// any.
        /// </summary>
        private bool TryIsolateFirstLiveEntry(
            out TKey key,
            out TValue value)
        {
            key = default(TKey);
            value = default(TValue);

            while (!IsEmpty
                && (!keyValuePair.Key.TryGetTarget(out key)
                    || !keyValuePair.Value.TryGetTarget(out value)))
            {
                DeleteFirst();
            }

            return !IsEmpty;
        }

        /// <summary>
        /// Cleans up all dead key-value pairs in this bucket.
        /// </summary>
        public void Cleanup()
        {
            TKey kvPairKey;
            TValue kvPairValue;
            if (!TryIsolateFirstLiveEntry(out kvPairKey, out kvPairValue))
            {
                return;
            }

            if (spilloverList != null)
            {
                var newSpilloverContents = spilloverList.contents;
                newSpilloverContents.Cleanup();
                if (newSpilloverContents.IsEmpty)
                {
                    spilloverList = null;
                }
                else
                {
                    spilloverList.contents = newSpilloverContents;
                }
            }
        }

        private HashSet<TKey> GetLiveKeys()
        {
            var keys = new HashSet<TKey>();
            AddLiveKeysTo(keys);
            return keys;
        }

        private void AddLiveKeysTo(HashSet<TKey> keys)
        {
            if (IsEmpty)
            {
                return;
            }

            TKey key;
            TValue value;

            if (keyValuePair.Key.TryGetTarget(out key)
                && keyValuePair.Value.TryGetTarget(out value))
            {
                keys.Add(key);
            }

            if (spilloverList != null)
            {
                spilloverList.contents.AddLiveKeysTo(keys);
            }
        }
    }

    internal sealed class WeakCacheBucketNode<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        public WeakCacheBucketNode(
            WeakCacheBucket<TKey, TValue> contents)
        {
            this.contents = contents;
        }

        public WeakCacheBucket<TKey, TValue> contents;
    }
}