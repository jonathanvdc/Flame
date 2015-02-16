using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class PushFloat32Instruction : LiteralInstruction<float>
    {
        public PushFloat32Instruction(ICodeGenerator CodeGenerator, float Value)
            : base(CodeGenerator, Value)
        {
        }

        public OpCode OpCode
        {
            get { return OpCodes.LoadFloat32; }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Context.Emit(OpCode, Value);
            TypeStack.Push(PrimitiveTypes.Float32);
        }
    }
    public class PushFloat64Instruction : LiteralInstruction<double>
    {
        public PushFloat64Instruction(ICodeGenerator CodeGenerator, double Value)
            : base(CodeGenerator, Value)
        {
        }

        public OpCode OpCode
        {
            get { return OpCodes.LoadFloat64; }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Context.Emit(OpCode, Value);
            TypeStack.Push(PrimitiveTypes.Float64);
        }
    }
}
