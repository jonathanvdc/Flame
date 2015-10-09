using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    public class DefinitionParser<TArg, T>
    {
        public DefinitionParser(IReadOnlyDictionary<string, Func<ParserState, LNode, TArg, INodeStructure<T>>> Parsers)
        {
            this.Parsers = Parsers;
        }

        public IReadOnlyDictionary<string, Func<ParserState, LNode, TArg, INodeStructure<T>>> Parsers { get; private set; }

        public DefinitionParser<TArg, T> WithParser(string Name, Func<ParserState, LNode, TArg, INodeStructure<T>> Parser)
        {
            var dict = Parsers.ToDictionary(item => item.Key, item => item.Value);
            dict[Name] = Parser;
            return new DefinitionParser<TArg, T>(dict);
        }

        public INodeStructure<T> Parse(ParserState State, LNode Node, TArg DeclaringMember)
        {
            return Parsers[Node.Name.Name](State, Node, DeclaringMember);
        }
    }
}
