using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class NodeList<T> : INodeStructure<IReadOnlyList<T>>
    {
        public NodeList(IReadOnlyList<INodeStructure<T>> Items)
        {
            this.Items = Items;
            this.cachedVal = new Lazy<T[]>(() => Items.Select(item => item.Value).ToArray());
        }

        public IReadOnlyList<INodeStructure<T>> Items { get; private set; }

        public LNode Node
        {
            get { return NodeFactory.Block(Items.Select(item => item.Node).ToArray()); }
        }

        private Lazy<T[]> cachedVal;
        public IReadOnlyList<T> Value
        {
            get { return cachedVal.Value; }
        }
    }
}
