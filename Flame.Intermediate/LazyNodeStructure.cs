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
        public LazyNodeStructure(LNode Node, Func<LNode, T> ValueFactory)
            : this(Node, () => ValueFactory(Node))
        { }
        public LazyNodeStructure(LNode Node, Func<T> ValueFactory)
            : this(Node, new Lazy<T>(ValueFactory))
        { }
        public LazyNodeStructure(LNode Node, Lazy<T> LazyValue)
        {
            this.Node = Node;
            this.LazyValue = LazyValue;
        }

        public LNode Node { get; private set; }

        public Lazy<T> LazyValue { get; private set; }
        public T Value
        {
            get { return LazyValue.Value; }
        }
    }
}
