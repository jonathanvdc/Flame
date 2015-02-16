using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class PopInstruction : ILInstruction
    {
        public PopInstruction(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            TypeStack.Pop();
            Context.Emit(OpCodes.Pop);
        }
    }
}
