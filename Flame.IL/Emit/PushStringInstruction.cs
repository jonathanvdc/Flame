using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class PushStringInstruction : LiteralInstruction<string>
    {
        public PushStringInstruction(ICodeGenerator CodeGenerator, string Value)
            : base(CodeGenerator, Value)
        {
        }

        public OpCode OpCode
        {
            get { return OpCodes.LoadString; }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Context.Emit(OpCode, Value);
            TypeStack.Push(PrimitiveTypes.String);
        }
    }
}
