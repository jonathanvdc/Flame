using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class DereferencePointerInstruction : OpCodeInstruction
    {
        public DereferencePointerInstruction(ICodeGenerator CodeGenerator, ICodeBlock Target)
            : base(CodeGenerator)
        {
            this.Target = (IInstruction)Target;
        }

        #region Static

        static DereferencePointerInstruction()
        {
            opCodeMapping = new Dictionary<IType, OpCode>()
            {
                { PrimitiveTypes.Int8, OpCodes.LoadAddressInt8 },
                { PrimitiveTypes.Int16, OpCodes.LoadAddressInt16 },
                { PrimitiveTypes.Int32, OpCodes.LoadAddressInt32 },
                { PrimitiveTypes.Int64, OpCodes.LoadAddressInt64 },
                { PrimitiveTypes.UInt8, OpCodes.LoadAddressUInt8 },
                { PrimitiveTypes.UInt16, OpCodes.LoadAddressUInt16 },
                { PrimitiveTypes.UInt32, OpCodes.LoadAddressUInt32 },
                { PrimitiveTypes.Bit8, OpCodes.LoadAddressUInt8 },
                { PrimitiveTypes.Bit16, OpCodes.LoadAddressUInt16 },
                { PrimitiveTypes.Bit32, OpCodes.LoadAddressUInt32 },
                { PrimitiveTypes.Float32, OpCodes.LoadAddressFloat32 },
                { PrimitiveTypes.Float64, OpCodes.LoadAddressFloat64 }
            };
        }

        private static Dictionary<IType, OpCode> opCodeMapping;

        #endregion

        public IInstruction Target { get; private set; }

        protected override OpCode GetOpCode(IType StackType)
        {
            var elemType = StackType.AsContainerType().GetElementType();
            if (opCodeMapping.ContainsKey(elemType))
            {
                return opCodeMapping[elemType];
            }
            else if (elemType.get_IsPointer())
            {
                return OpCodes.LoadAddressPointer;
            }
            else if (elemType.get_IsRootType())
            {
                return OpCodes.LoadAddressReference;
            }
            else
            {
                return OpCodes.LoadObject;
            }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Target.Emit(Context, TypeStack);
            var type = TypeStack.Peek();
            var op = GetOpCode(type);
            var elemType = type.AsContainerType().GetElementType();
            if (op.DataSize == 0)
            {
                Context.Emit(op);
            }
            else
            {
                Context.Emit(op, elemType);
            }
            TypeStack.Pop();
            TypeStack.Push(elemType);
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
        }
    }
}
