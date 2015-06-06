using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class PopBlock : ICecilBlock
    {
        public PopBlock(ICodeGenerator CodeGenerator, ICecilBlock Value)
        {
            this.Value = Value;
            this.CodeGenerator = CodeGenerator;
        }

        public ICecilBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public void Emit(IEmitContext Context)
        {
            int preDepth = Context.Stack.Count;
            Value.Emit(Context);
            if (preDepth > Context.Stack.Count)
            {
                var top = Context.Stack.Pop();
                if (!top.Equals(PrimitiveTypes.Void))
                {
                    Context.Emit(OpCodes.Pop);
                }
            }
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(1); }
        }
    }
}
