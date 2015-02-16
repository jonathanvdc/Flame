using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public abstract class TypedInstructionEmitterBase : ITypedInstructionEmitter
    {
        public void Emit(IEmitContext Context, IType Type)
        {
            if (Type.get_IsPointer())
            {
                Emit(Context, PointerOpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.Int8))
            {
                Emit(Context, Int8OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.Int16))
            {
                Emit(Context, Int16OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.Int32))
            {
                Emit(Context, Int32OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.Int64))
            {
                Emit(Context, Int64OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.UInt8))
            {
                Emit(Context, UInt8OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.UInt16))
            {
                Emit(Context, UInt16OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.UInt32))
            {
                Emit(Context, UInt32OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.UInt64))
            {
                Emit(Context, UInt64OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.Float32))
            {
                Emit(Context, Float32OpCode, Type);
            }
            else if (Type.Equals(PrimitiveTypes.Float64))
            {
                Emit(Context, Float64OpCode, Type);
            }
            else if (Type.get_IsRootType())
            {
                Emit(Context, ObjectOpCode, Type);
            }
            else
            {
                Emit(Context, DefaultOpCode, Type);
            }
        }
        protected static void Emit(IEmitContext Context, OpCode OpCode, IType Type)
        {
            if (OpCode.OperandType == OperandType.InlineType)
            {
                Context.Emit(OpCode, Type);
            }
            else
            {
                Context.Emit(OpCode);
            }
        }

        protected abstract OpCode PointerOpCode { get; }
        protected abstract OpCode Int8OpCode { get; }
        protected abstract OpCode Int16OpCode { get; }
        protected abstract OpCode Int32OpCode { get; }
        protected abstract OpCode Int64OpCode { get; }
        protected abstract OpCode UInt8OpCode { get; }
        protected abstract OpCode UInt16OpCode { get; }
        protected abstract OpCode UInt32OpCode { get; }
        protected abstract OpCode UInt64OpCode { get; }
        protected abstract OpCode Float32OpCode { get; }
        protected abstract OpCode Float64OpCode { get; }
        protected abstract OpCode ObjectOpCode { get; }
        protected abstract OpCode DefaultOpCode { get; }
    }
}
