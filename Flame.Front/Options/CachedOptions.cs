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
            this.cachedValues = new ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>();
            this.presentKeys = new ConcurrentDictionary<string, bool>();
        }

        public ICompilerOptions Options { get; private set; }

        private ConcurrentDictionary<string, bool> presentKeys;
        private ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> cachedValues;

        private ConcurrentDictionary<string, object> GetDictionary<T>()
        {
            return cachedValues.GetOrAdd(typeof(T), type => new ConcurrentDictionary<string, object>());
        }

        public T GetOption<T>(string Key, T Default)
        {
            if (HasOption(Key))
            {
                return (T)GetDictionary<T>().GetOrAdd(Key, key => Options.GetOption<T>(key, Default));
            }
            else
            {
                return Default;
            }
        }

        public bool HasOption(string Key)
        {
            return presentKeys.GetOrAdd(Key, Options.HasOption);
        }
    }
}
