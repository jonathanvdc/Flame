using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class PopStackBehavior : IStackBehavior
    {
        public PopStackBehavior(int PopCount)
        {
            this.PopCount = PopCount;
        }

        public int PopCount { get; private set; }

        public void Apply(TypeStack Stack)
        {
            for (int i = 0; i < PopCount; i++)
            {
                Stack.Pop();
            }
        }
    }
}
