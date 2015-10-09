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
            dict[Name] = Parser;
            return new ReferenceParser<T>(dict);
        }

        /// <summary>
        /// Figures out if the given node has a known node type,
        /// and can therefore be parsed.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public bool CanParse(LNode Node)
        {
            return Parsers.ContainsKey(Node.Name.Name);
        }

        public INodeStructure<T> Parse(ParserState State, LNode Node)
        {
            Func<ParserState, LNode, INodeStructure<T>> parser;
            if (Parsers.TryGetValue(Node.Name.Name, out parser))
            {
                return parser(State, Node);
            }
            else
            {
                throw new InvalidOperationException("Could not handle the given '" + Node.Name.Name + "' node because its type was unknown.");
            }
        }
    }
}
