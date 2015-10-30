using Flame.Compiler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public class CachedOptions : ICompilerOptions
    {
        public CachedOptions(ICompilerOptions Options)
        {
            this.Options = Options;
            this.cachedValues = new ConcurrentDictionary<Type, Dictionary<string, object>>();
        }

        public ICompilerOptions Options { get; private set; }

        private ConcurrentDictionary<Type, Dictionary<string, object>> cachedValues;

        private Dictionary<string, object> GetDictionary<T>()
        {
            return cachedValues.GetOrAdd(typeof(T), type => new Dictionary<string, object>());
        }

        public T GetOption<T>(string Key, T Default)
        {
            var dict = GetDictionary<T>();
            lock (dict)
            {
                if (!dict.ContainsKey(Key))
                {
                    if (HasOption(Key))
                    {
                        var result = Options.GetOption<T>(Key, Default);
                        dict[Key] = result;
                        return result;
                    }
                    else
                    {
                        return Default;
                    }
                }
                else
                {
                    return (T)dict[Key];
                }
            }
        }

        public bool HasOption(string Key)
        {
            return Options.HasOption(Key);
        }
    }
}
