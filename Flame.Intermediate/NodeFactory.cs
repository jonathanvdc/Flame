using Loyc;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public static class NodeFactory
    {
        static NodeFactory()
        {
            factory = new LNodeFactory(new EmptySourceFile("<synthetic>"));
        }

        private static LNodeFactory factory;

        public static LNode Id(string Name)
        {
            return factory.Id(Name);
        }

        public static LNode Call(string Target, IEnumerable<LNode> Arguments)
        {
            return factory.Call(GSymbol.Get(Target), Arguments);
        }

        public static LNode Call(LNode Target, IEnumerable<LNode> Arguments)
        {
            return factory.Call(Target, Arguments);
        }

        public static LNode Literal(object Value)
        {
            return factory.Literal(Value);
        }

        public static LNode Block(IEnumerable<LNode> Arguments)
        {
            return factory.Braces(Arguments);
        }
    }
}
