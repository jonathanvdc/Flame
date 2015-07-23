using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class LocalGetBlock : ICecilBlock
    {
        public LocalGetBlock(ILLocalVariable Local)
        {
            this.Local = Local;
        }

        public ILLocalVariable Local { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var emitVariable = Local.GetEmitLocal(Context);
            int index = emitVariable.Index;
            if (index == 0)
            {
                Context.Emit(OpCodes.Ldloc_0);
            }
            else if (index == 1)
            {
                Context.Emit(OpCodes.Ldloc_1);
            }
            else if (index == 2)
            {
                Context.Emit(OpCodes.Ldloc_2);
            }
            else if (index == 3)
            {
                Context.Emit(OpCodes.Ldloc_3);
            }
            else if (ILCodeGenerator.IsBetween(index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldloc_S, emitVariable);
            }
            else
            {
                Context.Emit(OpCodes.Ldloc, emitVariable);
            }
            Context.Stack.Push(BlockType);
        }

        public IType BlockType
        {
            get { return Local.Type; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Local.CodeGenerator; }
        }
    }
}
