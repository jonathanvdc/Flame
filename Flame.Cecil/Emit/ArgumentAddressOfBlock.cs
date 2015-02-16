using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ArgumentAddressOfBlock: ICecilBlock
    {
        public ArgumentAddressOfBlock(ICodeGenerator CodeGenerator, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (ILCodeGenerator.IsBetween(Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldarga_S, (byte)Index);
            }
            else
            {
                Context.Emit(OpCodes.Ldarga, Index);
            }
            StackBehavior.Apply(Context.Stack);
        }

        public IStackBehavior StackBehavior
        {
            get { return new SinglePushBehavior(ILCodeGenerator.GetExtendedParameterTypes(CodeGenerator)[Index].MakePointerType(PointerKind.ReferencePointer)); }
        }
    }
}
