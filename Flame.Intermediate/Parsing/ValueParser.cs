using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    /// <summary>
    /// A class that parses nodes based on their ids.
    /// </summary>
    public struct ValueParser<T>
    {
        public ValueParser(IReadOnlyDictionary<string, Func<ParserState, LNode, T>> Parsers)
        {
            this.Parsers = Parsers;
        }

        /// <summary>
        /// Gets a dictionary of node tags to parsers.
        /// </summary>
        /// <value>The parser dictionary.</value>
        public IReadOnlyDictionary<string, Func<ParserState, LNode, T>> Parsers { get; private set; }

        public ValueParser<T> WithParser(
            string Name, Func<ParserState, LNode, T> Parser)
        {
            var dict = Parsers.ToDictionary(item => item.Key, item => item.Value);
            dict[Name] = Parser;
            return new ValueParser<T>(dict);
        }

        public ValueParser<T> WithParsers(
            IEnumerable<KeyValuePair<string, Func<ParserState, LNode, T>>> Parsers)
        {
            var dict = this.Parsers.ToDictionary(item => item.Key, item => item.Value);
            foreach (var item in Parsers)
            {
                dict[item.Key] = item.Value;
            }
            return new ValueParser<T>(dict);
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

        /// <summary>
        /// Parse the specified node according to the given state.
        /// </summary>
        /// <param name="State">The state to parse the node with.</param>
        /// <param name="Node">The node to parse.</param>
        public T Parse(ParserState State, LNode Node)
        {
            Func<ParserState, LNode, T> parser;
            if (Parsers.TryGetValue(Node.Name.Name, out parser))
            {
                return parser(State, Node);
            }
            else if (Node.IsLiteral && Node.Value == null)
            {
                return default(T);
            }
            else
            {
                throw new InvalidOperationException(
                    "Could not handle the given '" + 
                    (Node.IsLiteral ? Node.Print() : Node.Name.Name) + 
                    "' node because its type was unknown.");
            }
        }
    }
}
