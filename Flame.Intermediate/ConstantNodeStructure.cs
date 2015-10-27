using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class ConstantNodeStructure<T> : INodeStructure<T>
    {
        public ConstantNodeStructure(LNode Node, T Value)
        {
            this.Node = Node;
            this.Value = Value;
        }

        public LNode Node { get; private set; }
        public T Value { get; private set; }
    }
}
