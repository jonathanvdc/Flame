using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public class StringCompilerOptions : ICompilerOptions
    {
        public StringCompilerOptions(IDictionary<string, string> Options)
        {
            this.options = new Dictionary<string, string>(Options);
        }
        public StringCompilerOptions()
        {
            this.options = new Dictionary<string, string>();
        }

        private Dictionary<string, string> options;

        public IReadOnlyDictionary<string, string> Options
        {
            get
            {
                return options;
            }
        }

        public bool GetFlag(string Key)
        {
            string lowerKey = this[Key].ToLower();
            return lowerKey == "true" || lowerKey == "yes" || lowerKey == "y";
        }
        public void SetFlag(string Key, bool Value)
        {
            this.options[Key] = Value ? "true" : "false";
        }

        public string this[string Key]
        {
            get
            {
                return options[Key];
            }
            set
            {
                this.options[Key] = value;
            }
        }

        public T GetOption<T>(string Key, T Default)
        {
            if (HasOption(Key))
            {
                var tType = typeof(T);
                if (tType == typeof(bool))
                {
                    return (T)(object)GetFlag(Key);
                }
                else if (tType == typeof(string))
                {
                    return (T)(object)options[Key];
                }
                else if (tType == typeof(int))
                {
                    return (T)(object)int.Parse(options[Key], CultureInfo.InvariantCulture);
                }
            }
            return Default;
        }

        public bool HasOption(string Key)
        {
            return this.options.ContainsKey(Key);
        }
    }
}
