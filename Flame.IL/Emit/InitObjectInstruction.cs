using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class InitObjectInstruction : OpCodeInstruction
    {
        public InitObjectInstruction(ICodeGenerator CodeGenerator, IType Type)
            : base(CodeGenerator)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        protected override OpCode GetOpCode(IType StackType)
        {
            return OpCodes.InitObject;
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            var opCode = GetOpCode(TypeStack.Peek());
            Context.Emit(opCode, Type);
            UpdateStack(TypeStack);
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
            TypeStack.Pop();
        }
    }
}
