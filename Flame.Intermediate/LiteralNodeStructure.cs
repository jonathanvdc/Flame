using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class LiteralNodeStructure<T> : INodeStructure<T>
    {
        public LiteralNodeStructure(T Value)
        {
            this.Value = Value;
        }

        public T Value { get; private set; }

        public Node Node
        {
            get { return NodeFactory.Literal(Value); }
        }
    }
}
