using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class ConcurrentMultiDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, IEnumerable<TValue>>
    {
        public ConcurrentMultiDictionary()
        {
            dict = new ConcurrentDictionary<TKey, ConcurrentBag<TValue>>();
        }

        private ConcurrentDictionary<TKey, ConcurrentBag<TValue>> dict;

        private ConcurrentBag<TValue> GetBag(TKey Key)
        {
            return dict.GetOrAdd(Key, _ => new ConcurrentBag<TValue>());
        }

        public void Add(TKey Key, TValue Value)
        {
            GetBag(Key).Add(Value);
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public IEnumerable<TKey> Keys
        {
            get { return dict.Keys; }
        }

        public bool TryGetValue(TKey key, out IEnumerable<TValue> value)
        {
            ConcurrentBag<TValue> result;
            bool success = dict.TryGetValue(key, out result);
            value = result;
            return success;
        }

        public IEnumerable<IEnumerable<TValue>> Values
        {
            get 
            {
                return dict.Values;
            }
        }

        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                return GetBag(key);
            }
        }

        public int Count
        {
            get { return dict.Count; }
        }

        public IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> GetEnumerator()
        {
            foreach (var item in dict)
                yield return new KeyValuePair<TKey, IEnumerable<TValue>>(
                    item.Key, item.Value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
