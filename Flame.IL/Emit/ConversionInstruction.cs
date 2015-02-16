using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ConversionInstruction : ILInstruction
    {
        public ConversionInstruction(ICodeGenerator CodeGenerator, OpCode OpCode, IType TypeArgument, IType TargetType)
            : base(CodeGenerator)
        {
            this.OpCode = OpCode;
            this.TargetType = TargetType;
            this.TypeArgument = TypeArgument;
        }
        public ConversionInstruction(ICodeGenerator CodeGenerator, OpCode OpCode, IType TargetType)
            : this(CodeGenerator, OpCode, TargetType, TargetType)
        {
        }

        public IType TypeArgument { get; private set; }
        public IType TargetType { get; private set; }

        public OpCode OpCode { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            TypeStack.Pop();
            if (OpCode.DataSize == 0)
            {
                Context.Emit(OpCode);
            }
            else
            {
                Context.Emit(OpCode, TypeArgument);
            }
            TypeStack.Push(TargetType);
        }
    }
}
