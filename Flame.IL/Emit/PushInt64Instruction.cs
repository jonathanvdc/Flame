using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class PushInt64Instruction : LiteralInstruction<long>
    {
        public PushInt64Instruction(ICodeGenerator CodeGenerator, long Value, IType Type)
            : base(CodeGenerator, Value)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public OpCode OpCode
        {
            get { return OpCodes.LoadInt64; }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Context.Emit(OpCode, Value);
            TypeStack.Push(Type);
        }
    }
}
