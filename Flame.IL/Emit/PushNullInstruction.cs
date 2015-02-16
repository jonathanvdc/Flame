using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class PushNullInstruction : ILInstruction
    {
        public PushNullInstruction(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        public OpCode OpCode
        {
            get { return OpCodes.LoadNull; }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Context.Emit(OpCode);
            TypeStack.Push(PrimitiveTypes.Null);
        }
    }
}
