using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class OffsetRegisterLocation : IUnmanagedStorageLocation
    {
        public OffsetRegisterLocation(ICodeGenerator CodeGenerator, IRegister Register, long Offset, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Register = Register;
            this.Offset = Offset;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IRegister Register { get; private set; }
        public long Offset { get; private set; }
        public IType Type { get; private set; }

        #region Load Methods

        public static OpCode GetFloatLoadOpCode(int Size)
        {
            if (Size == 4)
            {
                return OpCodes.LoadFloat32;
            }
            else if (Size == 8)
            {
                return OpCodes.LoadFloat64;
            }
            else
            {
                throw new InvalidOperationException("A store instruction for floating-point numbers of size " + Size + " does not exist.");
            }
        }

        public static OpCode GetLoadOpCode(IType Type)
        {            
            int size = Type.GetSize();
            if (Type.get_IsFloatingPoint())
            {
                return GetFloatLoadOpCode(size);
            }
            else
            {
                switch (size)
                {
                    case 1:
                        return (Type.get_IsUnsignedInteger() || Type.get_IsBit()) ? OpCodes.LoadUInt8 : OpCodes.LoadInt8;
                    case 2:
                        return (Type.get_IsUnsignedInteger() || Type.get_IsBit()) ? OpCodes.LoadUInt16 : OpCodes.LoadInt16;
                    case 4:
                        return OpCodes.LoadInt32;
                    default:
                        throw new InvalidOperationException("A load instruction for size " + size + " does not exist.");
                }
            }
        }

        public static OpCode GetFloatStoreOpCode(int Size)
        {
            if (Size == 4)
            {
                return OpCodes.StoreFloat32;
            }
            else if (Size == 8)
            {
                return OpCodes.StoreFloat64;
            }
            else
            {
                throw new InvalidOperationException("A store instruction for floating-point numbers of size " + Size + " does not exist.");
            }
        }

        public static OpCode GetStoreOpCode(IType Type)
        {
            int size = Type.GetSize();
            if (Type.get_IsFloatingPoint())
            {
                return GetFloatStoreOpCode(size);
            }
            else
            {
                switch (size)
                {
                    case 1:
                        return OpCodes.StoreInt8;
                    case 2:
                        return OpCodes.StoreInt16;
                    case 4:
                        return OpCodes.StoreInt32;
                    default:
                        throw new InvalidOperationException("A load instruction for size " + size + " does not exist.");
                }
            }
        }

        #endregion

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            var op = GetLoadOpCode(Type);
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
                context.Emit(new Instruction(op, context.ToArgument(Target), context.ToArgument(Offset, Register))));
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            var op = GetStoreOpCode(Type);
            return new ActionAssemblerBlock(CodeGenerator, (context) => 
                context.Emit(new Instruction(op, context.ToArgument(Target), context.ToArgument(Offset, Register))));
        }

        public IAssemblerBlock EmitLoadAddress(IRegister Target)
        {
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
                context.Emit(new Instruction(OpCodes.AddImmediate, context.ToArgument(Target), context.ToArgument(Offset, Register))));
        }

        public IAssemblerBlock EmitRelease()
        {
            return new EmptyBlock(CodeGenerator);
        }
    }
}
