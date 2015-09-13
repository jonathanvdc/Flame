using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class AsyncDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, Task<TValue>>
    {
        public AsyncDictionary()
        {
            dict = new ConcurrentDictionary<TKey, Task<TValue>>();
        }

        private ConcurrentDictionary<TKey, Task<TValue>> dict;

        public Task<TValue> GetOrAdd(TKey Key, TValue InitialValue, Func<TKey, Task<TValue>> ValueFactory)
        {
            return dict.GetOrAdd(Key, key =>
            {
                dict[key] = Task.FromResult(InitialValue);
                var result = ValueFactory(key);
                dict[key] = result;
                return result;
            });
        }

        public Task<TValue> GetOrAdd(TKey Key, Func<TKey, Task<TValue>> ValueFactory)
        {
            return dict.GetOrAdd(Key, ValueFactory);
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public IEnumerable<TKey> Keys
        {
            get { return dict.Keys; }
        }

        public bool TryGetValue(TKey key, out Task<TValue> value)
        {
            return dict.TryGetValue(key, out value);
        }

        public IEnumerable<Task<TValue>> Values
        {
            get 
            {
                return dict.Values;
            }
        }

        public Task<TValue> this[TKey key]
        {
            get
            {
                return dict[key];
            }
        }

        public int Count
        {
            get { return dict.Count; }
        }

        public IEnumerator<KeyValuePair<TKey, Task<TValue>>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
