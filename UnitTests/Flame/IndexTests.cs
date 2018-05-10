using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using Flame.Collections;

namespace UnitTests
{
    [TestFixture]
    public class IndexTests
    {
        public IndexTests(Random rng)
        {
            this.rng = rng;
        }

        private Random rng;

        [Test]
        public void IntDictionaryComparison()
        {
            DictionaryComparison<int, int>(EqualityComparer<int>.Default, rng.Next, rng.Next, 10000);
        }

        [Test]
        public void IntSeqDictionaryComparison()
        {
            DictionaryComparison<IEnumerable<int>, int>(
                EnumerableComparer<int>.Default,
                () => rng.NextArray<int>(rng.Next(0, 20), r => r.Next()),
                rng.Next,
                10000);
        }

        private void DictionaryComparison<TKey, TValue>(
            IEqualityComparer<TKey> keyComparer,
            Func<TKey> nextKey,
            Func<TValue> nextValue,
            int elemCount)
        {
            // Create a dictionary and fill it up with key-value pairs.
            var dict = new Dictionary<TKey, TValue>(keyComparer);
            for (int i = 0; i < elemCount; i++)
            {
                dict[nextKey()] = nextValue();
            }

            // Create an index.
            var index = new Index<Dictionary<TKey, TValue>, TKey, TValue>(d => d);

            // Check that all values are indeed in the index.
            foreach (var kvPair in dict)
            {
                Assert.IsTrue(
                    index.GetAll(dict, kvPair.Key)
                    .SequenceEqual(new[] { kvPair.Value }));
            }

            // Check that other keys are not in the index.
            for (int i = 0; i < 1000; i++)
            {
                var key = nextKey();
                if (!dict.ContainsKey(key))
                {
                    Assert.IsEmpty(
                        index.GetAll(dict, key));
                }
            }
        }
    }
}