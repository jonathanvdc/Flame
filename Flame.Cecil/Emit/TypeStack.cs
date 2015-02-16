using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class TypeStack
    {
        public TypeStack()
        {
            this.stack = new Stack<IType>();
        }
        public TypeStack(Stack<IType> Stack)
        {
            this.stack = new Stack<IType>(Stack.Reverse());
        }
        public TypeStack(TypeStack Other)
        {
            this.stack = new Stack<IType>(Other.stack.Reverse());
        }

        private Stack<IType> stack;

        public IType Pop()
        {
            return stack.Pop();
        }
        public void Push(IType Type)
        {
            stack.Push(Type);
        }
        public IType Peek()
        {
            return stack.Peek();
        }

        public int Count
        {
            get
            {
                return stack.Count;
            }
        }
    }
}
