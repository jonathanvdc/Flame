using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public class StringCompilerOptions : ICompilerOptions
    {
        public StringCompilerOptions(IDictionary<string, string> Options, IOptionParser<string> Parser)
        {
            this.options = new Dictionary<string, string>(Options);
            OptionParser = Parser;
        }
        public StringCompilerOptions(IDictionary<string, string> Options)
        {
            this.options = new Dictionary<string, string>(Options);
            OptionParser = new StringOptionParser();
        }
        public StringCompilerOptions()
        {
            this.options = new Dictionary<string, string>();
            OptionParser = new StringOptionParser();
        }

        public IOptionParser<string> OptionParser { get; private set; }

        private Dictionary<string, string> options;

        public IReadOnlyDictionary<string, string> Options
        {
            get
            {
                return options;
            }
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
            if (HasOption(Key) && OptionParser.CanParse<T>())
            {
                return OptionParser.ParseValue<T>(this[Key]);
            }
            return Default;
        }

        public bool HasOption(string Key)
        {
            return this.options.ContainsKey(Key);
        }
    }
}
