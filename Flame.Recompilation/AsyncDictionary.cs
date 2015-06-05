using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class AsyncDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        public AsyncDictionary()
        {
            dict = new Dictionary<TKey, Task<TValue>>();
        }

        private Dictionary<TKey, Task<TValue>> dict;

        public void Add(TKey Key, Task<TValue> Value)
        {
            dict.Add(Key, Value);
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public IEnumerable<TKey> Keys
        {
            get { return dict.Keys; }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (ContainsKey(key))
            {
                value = this[key];
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public IEnumerable<TValue> Values
        {
            get 
            {
                var task = Task.WhenAll(dict.Values);
                task.Wait();
                return task.Result;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                var task = dict[key];
                task.Wait();
                return task.Result;
            }
        }

        public int Count
        {
            get { return dict.Count; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var task = Task.WhenAll(dict.Select(item => item.Value.ContinueWith(val => new KeyValuePair<TKey, TValue>(item.Key, val.Result))));
            task.Wait();
            return task.Result.AsEnumerable().GetEnumerator(); 
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
