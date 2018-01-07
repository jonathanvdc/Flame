using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using Flame;
using Flame.Collections;

namespace UnitTests
{
    [TestFixture]
    public class CacheTests
    {
        public CacheTests(Random rng)
        {
            this.rng = rng;
        }

        private Random rng;

        [Test]
        public void LruCache()
        {
            new CacheStressTester<int, int>(rng).TestCache(
                new LruCache<int, int>(128),
                GenerateInt32,
                GenerateInt32,
                true);
        }

        [Test]
        public void WeakCache()
        {
            new CacheStressTester<object, object>(rng).TestCache(
                new WeakCache<object, object>(EqualityComparer<object>.Default),
                GenerateInt32Object,
                GenerateInt32Object,
                false);
        }

        [Test]
        public void WeakCacheCleanup()
        {
            // Create a weak cache.
            var newCache = new WeakCache<object, object>(EqualityComparer<object>.Default);
            const int opCount = 40000;
            // Insert new values and re-shuffle old values. Just check that the cache
            // cleanup code doesn't cause exceptions.
            for (int i = 0; i < opCount; i++)
            {
                newCache.Insert(i, GenerateInt32Object(rng));

                object value;
                if (newCache.TryGet(GenerateInt32Object(rng), out value))
                {
                    newCache.Insert(GenerateInt32Object(rng), value);
                }
            }
        }

        private int GenerateInt32(Random rng)
        {
            return rng.Next(short.MinValue, short.MaxValue);
        }

        private object GenerateInt32Object(Random rng)
        {
            return GenerateInt32(rng);
        }
    }

    internal class CacheStressTester<TKey, TValue>
    {
        public CacheStressTester(Random rng)
        {
            this.rng = rng;
        }

        private Random rng;

        public void TestCache(
            Cache<TKey, TValue> cache,
            Func<Random, TKey> generateKey,
            Func<Random, TValue> generateValue,
            bool relaxHasKey)
        {
            var dict = new Dictionary<TKey, TValue>();
            const int opCount = 40000;
            for (int i = 0; i < opCount; i++)
            {
                int op = rng.Next(3);
                if (op == 0)
                {
                    // Perform an insertion.
                    var key = generateKey(rng);
                    var value = generateValue(rng);
                    dict[key] = value;
                    cache.Insert(key, value);
                }
                else if (op == 1)
                {
                    // Perform a try-get operation.
                    var key = generateKey(rng);
                    TValue value, cacheValue;
                    var hasKey = dict.TryGetValue(key, out value);
                    var cacheHasKey = cache.TryGet(key, out cacheValue);
                    if (!relaxHasKey)
                    {
                        Assert.AreEqual(
                            hasKey,
                            cacheHasKey,
                            "Try-get operation error: cache says " + 
                            "it does not contain key '" + key +
                            "', but it should.");
                    }
                    if (cacheHasKey)
                    {
                        Assert.AreEqual(
                            value,
                            cacheValue,
                            "Try-get operation error: cached value '" + cacheValue +
                            "' does not match actual value '" + value +
                            "' (key: '" + key + "').");
                    }
                }
                else
                {
                    // Perform a get operation.
                    var key = generateKey(rng);
                    TValue value;
                    if (!dict.TryGetValue(key, out value))
                    {
                        value = generateValue(rng);
                        dict[key] = value;
                    }
                    var cacheValue = cache.Get(key, new ConstantFunction<TKey, TValue>(value).Apply);
                    Assert.AreEqual(
                        value,
                        cacheValue,
                        "Get operation error: cached value '" + cacheValue +
                        "' does not match actual value '" + value +
                        "' (key: '" + key + "').");
                }
            }
        }
    }

    internal class ConstantFunction<T1, T2>
    {
        public ConstantFunction(T2 result)
        {
            this.Result = result;
        }

        public T2 Result { get; private set; }

        public T2 Apply(T1 arg)
        {
            return Result;
        }
    }
}
