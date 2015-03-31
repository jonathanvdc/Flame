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
            return Parser.CanParse<T>();
        }
    }
}
