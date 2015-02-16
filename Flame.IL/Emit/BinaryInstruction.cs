using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class BinaryInstruction : ILInstruction
    {
        public BinaryInstruction(ICodeGenerator CodeGenerator, OpCode OpCode, ICodeBlock LeftOperand, ICodeBlock RightOperand)
            : base(CodeGenerator)
        {
            this.OpCode = OpCode;
            this.LeftOperand = (IInstruction)LeftOperand;
            this.RightOperand = (IInstruction)RightOperand;
        }

        public IInstruction LeftOperand { get; private set; }
        public IInstruction RightOperand { get; private set; }

        public OpCode OpCode { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            LeftOperand.Emit(Context, TypeStack);
            RightOperand.Emit(Context, TypeStack);
            Context.Emit(OpCode);
            TypeStack.Pop();
        }
    }
}
