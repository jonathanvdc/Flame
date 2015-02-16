using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class UnaryInstruction : ILInstruction
    {
        public UnaryInstruction(ICodeGenerator CodeGenerator, OpCode OpCode, ICodeBlock Operand)
            : base(CodeGenerator)
        {
            this.OpCode = OpCode;
            this.Operand = (IInstruction)Operand;
        }

        public IInstruction Operand { get; private set; }
        public OpCode OpCode { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Operand.Emit(Context, TypeStack);
            Context.Emit(OpCode);
        }
    }
}
