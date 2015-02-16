using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class IfElseStackBehavior : IStackBehavior
    {
        public IfElseStackBehavior(IStackBehavior IfBehavior, IStackBehavior ElseBehavior)
        {
            this.IfBehavior = IfBehavior;
            this.ElseBehavior = ElseBehavior;
        }

        public IStackBehavior IfBehavior { get; private set; }
        public IStackBehavior ElseBehavior { get; private set; }

        public void Apply(TypeStack Stack)
        {
            IfBehavior.Apply(Stack);
            var stackCopy = new TypeStack(Stack);
            ElseBehavior.Apply(stackCopy);
            if (Stack.Count != stackCopy.Count)
            {
                throw new InvalidOperationException("Asymmetric stack changes are not allowed for if-else statements.");
            }
        }
    }
}
