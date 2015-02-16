using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ArgumentSetBlock : ICecilBlock
    {
        public ArgumentSetBlock(ICodeGenerator CodeGenerator, int Index, ICecilBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
            this.Value = Value;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }
        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            Context.Stack.Pop();
            if (ILCodeGenerator.IsBetween(Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Starg_S, (byte)Index);
            }
            else
            {
                Context.Emit(OpCodes.Starg, Index);
            }
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(0); }
        }
    }
}
