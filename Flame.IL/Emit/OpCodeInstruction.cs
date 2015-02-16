using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public abstract class OpCodeInstruction : ILInstruction
    {
        public OpCodeInstruction(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        protected abstract OpCode GetOpCode(IType StackType);

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            var opCode = GetOpCode(TypeStack.Peek());
            Context.Emit(opCode);
            UpdateStack(TypeStack);
        }

        protected abstract void UpdateStack(Stack<IType> TypeStack);
    }
}
