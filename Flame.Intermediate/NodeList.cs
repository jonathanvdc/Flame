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
        }

        public IReadOnlyList<INodeStructure<T>> Items { get; private set; }

        public Node Node
        {
            get { return NodeFactory.Block(Items.Select(item => item.Node).ToArray()); }
        }

        public IReadOnlyList<T> Value
        {
            get { return Items.Select(item => item.Value).ToArray(); }
        }
    }
}
