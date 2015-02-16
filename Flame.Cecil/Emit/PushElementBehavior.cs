using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class PushElementBehavior : IStackBehavior
    {
        public void Apply(TypeStack Stack)
        {
            var ptr = Stack.Pop();
            Stack.Push(ptr.AsContainerType().GetElementType());
        }
    }
}
