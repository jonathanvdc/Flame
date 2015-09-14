using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public static class NodeFactory
    {
        public const string BlockNodeName = "{}";

        public static Node Id(string Name)
        {
            return new IdNode(Name);
        }

        public static Node Call(string Target, IReadOnlyList<Node> Arguments)
        {
            return new CallNode(Target, Arguments);
        }

        public static Node Call(Node Target, IReadOnlyList<Node> Arguments)
        {
            return new CallNode(Target, Arguments);
        }

        public static Node Literal(object Value)
        {
            return new LiteralNode(Value);
        }

        public static Node Block(IReadOnlyList<Node> Arguments)
        {
            return Call(BlockNodeName, Arguments);
        }
    }
}
