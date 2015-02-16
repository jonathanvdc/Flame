using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class InvocationStackBehavior : IStackBehavior
    {
        public InvocationStackBehavior(IStackBehavior MethodBehavior, IEnumerable<IStackBehavior> ArgumentsBehavior)
        {
            this.MethodBehavior = MethodBehavior;
            this.ArgumentsBehavior = ArgumentsBehavior;
        }

        public IStackBehavior MethodBehavior { get; private set; }
        public IEnumerable<IStackBehavior> ArgumentsBehavior { get; private set; }

        public void Apply(TypeStack Stack)
        {
            MethodBehavior.Apply(Stack);
            var method = (IMethod)Stack.Pop();
            foreach (var item in ArgumentsBehavior)
            {
                item.Apply(Stack);
                Stack.Pop();
            }
            Stack.Push(method.ReturnType);
        }
    }
}
