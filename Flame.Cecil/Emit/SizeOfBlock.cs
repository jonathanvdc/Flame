using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class SizeOfBlock : ICecilBlock
    {
        public SizeOfBlock(ICodeGenerator CodeGenerator, IType TargetType)
        {
            this.CodeGenerator = CodeGenerator;
            this.TargetType = TargetType;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType TargetType { get; private set; }

        public void Emit(IEmitContext Context)
        {
            if (TargetType.get_IsPrimitive())
            {
                ((ICecilBlock)CodeGenerator.EmitInt32(TargetType.GetPrimitiveSize())).Emit(Context);
            }
            else
            {
                Context.Emit(OpCodes.Sizeof, TargetType);
                Context.Stack.Push(PrimitiveTypes.Int32);
            }
        }

        public IStackBehavior StackBehavior
        {
            get { return new SinglePushBehavior(PrimitiveTypes.Int32); }
        }
    }
}
