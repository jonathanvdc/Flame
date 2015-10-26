using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class LazyNodeStructure<T> : INodeStructure<T>
    {
        public LazyNodeStructure(T Value, Func<T, LNode> NodeFactory)
            : this(Value, () => NodeFactory(Value))
        { }
        public LazyNodeStructure(T Value, Func<LNode> NodeFactory)
            : this(Value, new Lazy<LNode>(NodeFactory))
        { }
        public LazyNodeStructure(T Value, Lazy<LNode> LazyNode)
        {
            this.Value = Value;
            this.LazyNode = LazyNode;
        }

        public Lazy<LNode> LazyNode { get; private set; }
        public LNode Node
        {
            get { return LazyNode.Value; }
        }

        public T Value { get; private set; }
    }
}
