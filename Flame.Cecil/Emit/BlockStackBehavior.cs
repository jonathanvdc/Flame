using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BlockStackBehavior : IStackBehavior
    {
        public BlockStackBehavior(params IStackBehavior[] Children)
        {
            this.Children = Children;
        }
        public BlockStackBehavior(IEnumerable<IStackBehavior> Children)
        {
            this.Children = Children;
        }

        public IEnumerable<IStackBehavior> Children { get; private set; }

        public void Apply(TypeStack Stack)
        {
            foreach (var item in Children)
            {
                item.Apply(Stack);
            }
        }
    }
}
