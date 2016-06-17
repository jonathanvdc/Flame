using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RandomAccessOptions : IRandomAccessOptions
    {
        public RandomAccessOptions()
        {
            values = new Dictionary<string, object>();
        }

        private Dictionary<string, object> values;

        public void SetOption<T>(string Key, T Value)
        {
            values[Key] = Value;
        }

        public T GetOption<T>(string Key, T Default)
        {
            if (values.ContainsKey(Key))
            {
                object result = values[Key];
                if (result is T)
                {
                    return (T)result;
                }
            }
            return Default;
        }

        public T GetOrCreateOption<T>(string Key, Func<T> Factory)
        {
            if (values.ContainsKey(Key))
            {
                return (T)values[Key];
            }
            else
            {
                var result = Factory();
                values[Key] = result;
                return result;
            }
        }

        public bool HasOption(string Key)
        {
            return values.ContainsKey(Key);
        }
    }
}
