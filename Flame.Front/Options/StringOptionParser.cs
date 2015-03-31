using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public class StringOptionParser : IOptionParser<string>
    {
        public StringOptionParser()
        {
            parsers = new Dictionary<Type, Func<string, object>>();
        }

        #region Static

        public static void RegisterPrimitiveParsers(StringOptionParser Parser)
        {
            Parser.RegisterParser<bool>(ParseFlag);
            Parser.RegisterParser<string>(item => item);
            Parser.RegisterParser<char>(char.Parse);
            Parser.RegisterParser<sbyte>(item => sbyte.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<short>(item => short.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<int>(item => int.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<long>(item => long.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<byte>(item => byte.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<ushort>(item => ushort.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<uint>(item => uint.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<ulong>(item => ulong.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<double>(item => double.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<float>(item => float.Parse(item, CultureInfo.InvariantCulture));
            Parser.RegisterParser<decimal>(item => decimal.Parse(item, CultureInfo.InvariantCulture));
        }

        public static void RegisterCompilerParsers(StringOptionParser Parser)
        {
            Parser.RegisterParser<ChatLevel>(ChatLogFilter.ParseChatLevel);
            Parser.RegisterParser<ILogFilter>(ChatLogFilter.ParseLogFilter);
            Parser.RegisterParser<PathIdentifier>(PathIdentifier.Parse);
        }

        public static StringOptionParser CreateDefault()
        {
            var handler = new StringOptionParser();
            RegisterPrimitiveParsers(handler);
            RegisterCompilerParsers(handler);
            return handler;
        }

        public static bool ParseFlag(string Flag)
        {
            string lowerVal = Flag.ToLower();
            return lowerVal == "true" || lowerVal == "yes" || lowerVal == "y"; 
        }

        #endregion

        private Dictionary<Type, Func<string, object>> parsers;
        public Func<string, T> GetParser<T>()
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
        public void RegisterParser<T>(Func<string, T> Parser)
        {
            parsers[typeof(T)] = (item) => Parser(item);
        }
        public T ParseValue<T>(string Value)
        {
            return GetParser<T>()(Value);
        }

        public bool CanParse<T>()
        {
            return parsers.ContainsKey(typeof(T));
        }
    }
}
