using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Loyc.MiniTest;
using Flame;
using Flame.Collections;
using Loyc;
using System.Threading;

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
            var time = new CacheStressTester<int, int>(rng).TestCache(
                new LruCache<int, int>(128),
                GenerateInt32,
                GenerateInt32,
                true);
            Console.WriteLine("LRU cache test took " + time);
        }

        [Test]
        public void WeakCache()
        {
            var time = new CacheStressTester<object, object>(rng).TestCache(
                new WeakCache<object, object>(EqualityComparer<object>.Default),
                GenerateInt32Object,
                GenerateInt32Object,
                false);
            Console.WriteLine("Weak cache test took " + time);
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

        [Test]
        public void WeakCacheCleanup2()
        {
            int iterations = 100;
            var cache = new WeakCache<object, object>();
            for (int i = 0; i < iterations; i++)
            {
                new CacheStressTester<object, object>(
                    rng,
                    CacheStressTester<object, object>.DefaultOpCount / iterations)
                    .TestCache(
                        cache,
                        GenerateInt32Object,
                        GenerateInt32Object,
                        false);
                GC.Collect();
            }
        }

        [Test]
        public void WeakCacheConcurrent()
        {
            int threadCount = 16;

            var cache = new WeakCache<object, object>();
            var tests = new ConcurrentCacheTest[threadCount];
            var threads = new Thread[threadCount];
            int delta = (short.MaxValue - short.MinValue) / threadCount;

            var timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < threadCount; i++)
            {
                tests[i] = new ConcurrentCacheTest(
                    cache,
                    new Random(rng.Next()),
                    short.MinValue + i * delta,
                    short.MinValue + (i + 1) * delta);

                threads[i] = new Thread(tests[i].Run);
                threads[i].Start();
            }

            var totalTime = new TimeSpan(0);
            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Join();
                totalTime += tests[i].RunTime;
            }
            timer.Stop();

            Console.WriteLine(
                "Concurrent cache test wall clock time: " + timer.Elapsed);
            Console.WriteLine(
                "Concurrent cache test total time: " + totalTime);
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
        public CacheStressTester(Random rng, int opCount)
        {
            this.rng = rng;
            this.opCount = opCount;
        }

        public CacheStressTester(Random rng)
            : this(rng, DefaultOpCount)
        { }

        private Random rng;
        private int opCount;

        public static readonly int DefaultOpCount = 10000;

        public TimeSpan TestCache(
            Cache<TKey, TValue> cache,
            Func<Random, TKey> generateKey,
            Func<Random, TValue> generateValue,
            bool relaxHasKey)
        {
            var perfTimer = new Stopwatch();
            perfTimer.Start();

            var dict = new Dictionary<TKey, TValue>();
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

            perfTimer.Stop();
            return perfTimer.Elapsed;
        }
    }

    internal class ConcurrentCacheTest
    {
        public ConcurrentCacheTest(
            WeakCache<object, object> cache,
            Random rng,
            int rangeStart,
            int rangeEnd)
        {
            this.cache = cache;
            this.rng = rng;
            this.rangeStart = rangeStart;
            this.rangeEnd = rangeEnd;
        }

        private WeakCache<object, object> cache;
        private Random rng;
        private int rangeStart;
        private int rangeEnd;

        public TimeSpan RunTime { get; private set; }

        public void Run()
        {
            var tester = new CacheStressTester<object, object>(rng);

            // TODO: figure out why try-get sometimes returns 'false'
            // when it should return 'true' in a multi-threaded setting.
            RunTime = tester.TestCache(
                cache,
                generateKey,
                generateValue,
                true);
        }

        private object generateKey(Random rng)
        {
            return rng.Next(rangeStart + 1, rangeEnd - 1);
        }

        private object generateValue(Random rng)
        {
            return rng.Next();
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
