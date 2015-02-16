using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class SinglePushBehavior : IStackBehavior
    {
        public SinglePushBehavior(IType Type)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public void Apply(TypeStack Stack)
        {
            Stack.Push(Type);
        }
    }
}
