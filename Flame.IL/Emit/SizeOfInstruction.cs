using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class SizeOfInstruction : ILInstruction
    {
        public SizeOfInstruction(ICodeGenerator CodeGenerator, IType Type)
            : base(CodeGenerator)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public OpCode OpCode
        {
            get { return OpCodes.SizeOf; }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Context.Emit(OpCode, Type);
            TypeStack.Push(PrimitiveTypes.Int32);
        }
    }
}
