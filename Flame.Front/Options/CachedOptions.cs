using Flame.Compiler;
using System;
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
            this.cachedValues = new Dictionary<Type, Dictionary<string, object>>();
        }

        public ICompilerOptions Options { get; private set; }

        private Dictionary<Type, Dictionary<string, object>> cachedValues;

        private Dictionary<string, object> GetDictionary<T>()
        {
            var type = typeof(T);
            if (cachedValues.ContainsKey(type))
            {
                return cachedValues[type];
            }
            else
            {
                var newDict = new Dictionary<string, object>();
                cachedValues[type] = newDict;
                return newDict;
            }
        }

        public T GetOption<T>(string Key, T Default)
        {
            var dict = GetDictionary<T>();
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

        public bool HasOption(string Key)
        {
            return Options.HasOption(Key);
        }
    }
}
