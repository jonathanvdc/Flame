using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ReturnInstruction : ILInstruction
    {
        public ReturnInstruction(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            if (CodeGenerator.Method.ReturnType != null && !CodeGenerator.Method.ReturnType.Equals(PrimitiveTypes.Void))
            {
                TypeStack.Pop();
            }
            Context.Emit(OpCodes.Return);
        }
    }
}
