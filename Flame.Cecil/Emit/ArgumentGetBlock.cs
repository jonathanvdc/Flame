using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ArgumentGetBlock : ICecilBlock
    {
        public ArgumentGetBlock(ICodeGenerator CodeGenerator, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (Index == 0)
            {
                Context.Emit(OpCodes.Ldarg_0);
            }
            else if (Index == 1)
            {
                Context.Emit(OpCodes.Ldarg_1);
            }
            else if (Index == 2)
            {
                Context.Emit(OpCodes.Ldarg_2);
            }
            else if (Index == 3)
            {
                Context.Emit(OpCodes.Ldarg_3);
            }
            else if (ILCodeGenerator.IsBetween(Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldarg_S, (byte)Index);
            }
            else
            {
                Context.Emit(OpCodes.Ldarg, Index);
            }
            StackBehavior.Apply(Context.Stack);
        }

        public IStackBehavior StackBehavior
        {
            get { return new SinglePushBehavior(ILCodeGenerator.GetExtendedParameterTypes(CodeGenerator)[Index]); }
        }
    }
}
