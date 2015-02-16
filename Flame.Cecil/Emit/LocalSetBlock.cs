using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class LocalSetBlock : ICecilBlock
    {
        public LocalSetBlock(ILLocalVariable LocalVariable, ICecilBlock Value)
        {
            this.LocalVariable = LocalVariable;
            this.Value = Value;
        }

        public ICodeGenerator CodeGenerator { get { return LocalVariable.CodeGenerator; } }
        public ILLocalVariable LocalVariable { get; private set; }
        public ICecilBlock Value { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            Context.Stack.Pop();
            var emitVariable = LocalVariable.GetEmitLocal(Context);
            int index = emitVariable.Index;
            if (index == 0)
            {
                Context.Emit(OpCodes.Stloc_0);
            }
            else if (index == 1)
            {
                Context.Emit(OpCodes.Stloc_1);
            }
            else if (index == 2)
            {
                Context.Emit(OpCodes.Stloc_2);
            }
            else if (index == 3)
            {
                Context.Emit(OpCodes.Stloc_3);
            }
            else if (ILCodeGenerator.IsBetween(index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Stloc_S, emitVariable);
            }
            else
            {
                Context.Emit(OpCodes.Stloc, emitVariable);
            }
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(0); }
        }
    }
}
