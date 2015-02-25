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

        #region Static

        static StringCompilerOptions()
        {
            parsers = new Dictionary<Type, Func<string, object>>();
            RegisterParser<bool>(ParseFlag);
            RegisterParser<string>((item) => item);
            RegisterParser<char>(char.Parse);
            RegisterParser<sbyte>((item) => sbyte.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<short>((item) => short.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<int>((item) => int.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<long>((item) => long.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<byte>((item) => byte.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<ushort>((item) => ushort.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<uint>((item) => uint.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<ulong>((item) => ulong.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<double>((item) => double.Parse(item, CultureInfo.InvariantCulture));
            RegisterParser<float>((item) => float.Parse(item, CultureInfo.InvariantCulture));

            RegisterParser<Flame.CodeDescription.IDocumentationFormatter>((item) =>
            {
                switch (item.ToLower())
                {
                    case "doxygen":
                        return new Flame.CodeDescription.DoxygenFormatter();
                    case "xml":
                        return Flame.CodeDescription.XmlDocumentationFormatter.Instance;
                    default:
                        return Flame.CodeDescription.DefaultDocumentationFormatter.Instance;
                }
            });
        }

        public static bool ParseFlag(string Flag)
        {
            string lowerVal = Flag.ToLower();
            return lowerVal == "true" || lowerVal == "yes" || lowerVal == "y"; 
        }

        private static Dictionary<Type, Func<string, object>> parsers;
        public static Func<string, T> GetParser<T>()
        {
            var type = typeof(T);
            if (parsers.ContainsKey(type))
            {
                return (val) => (T)parsers[type](val);
            }
            else
            {
                return null;
            }
        }
        public static void RegisterParser<T>(Func<string, T> Parser)
        {
            parsers[typeof(T)] = (item) => Parser(item);
        }
        public static T ParseValue<T>(string Value)
        {
            return GetParser<T>()(Value);
        }

        #endregion

        public T GetOption<T>(string Key, T Default)
        {
            if (HasOption(Key))
            {
                return ParseValue<T>(this[Key]);
            }
            return Default;
        }

        public bool HasOption(string Key)
        {
            return this.options.ContainsKey(Key);
        }
    }
}
