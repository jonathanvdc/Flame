using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    public class ReferenceParser<T>
    {
        public ReferenceParser(IReadOnlyDictionary<string, Func<ParserState, LNode, INodeStructure<T>>> Parsers)
        {
            this.Parsers = Parsers;
        }

        public IReadOnlyDictionary<string, Func<ParserState, LNode, INodeStructure<T>>> Parsers { get; private set; }

        public ReferenceParser<T> WithParser(string Name, Func<ParserState, LNode, INodeStructure<T>> Parser)
        {
            var dict = Parsers.ToDictionary(item => item.Key, item => item.Value);
            dict.Add(Name, Parser);
            return new ReferenceParser<T>(dict);
        }

        public INodeStructure<T> Parse(ParserState State, LNode Node)
        {
            return Parsers[Node.Name.Name](State, Node);
        }
    }
}
