using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public class StringArrayOptionParser : IOptionParser<string[]>
    {
        public StringArrayOptionParser(IOptionParser<string> Parser)
        {
            this.Parser = Parser;
        }

        public IOptionParser<string> Parser { get; private set; }

        public T ParseValue<T>(string[] Value)
        {
            if (typeof(T).IsArray)
            {
                var optParserType = typeof(IOptionParser<string>);
                var elemType = typeof(T).GetElementType();
                var genericMethod = optParserType.GetMethod("ParseValue").MakeGenericMethod(elemType);
                Array results = (Array)Activator.CreateInstance(typeof(T), Value.Length);
                for (int i = 0; i < results.Length; i++)
                {
                    results.SetValue(genericMethod.Invoke(Parser, new object[] { Value[i] }), i);
                }
                return (T)(object)results;
            }
            if (Value.Length == 0)
            {
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)true;
                }
                else
                {
                    return default(T);
                }
            }
            else
            {
                return Parser.ParseValue<T>(Value[0]);
            }
        }

        public bool CanParse<T>()
        {
            return Parser.CanParse<T>() || 
                (typeof(T).IsArray && 
                (bool)typeof(IOptionParser<string>).GetMethod("CanParse").MakeGenericMethod(typeof(T).GetElementType()).Invoke(Parser, new object[0]));
        }
    }
}
