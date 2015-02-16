using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class StoreAtAddressInstruction : OpCodeInstruction
    {
        public StoreAtAddressInstruction(ICodeGenerator CodeGenerator, ICodeBlock Target, ICodeBlock Value)
            : base(CodeGenerator)
        {
            this.Target = (IInstruction)Target;
            this.Value = (IInstruction)Value;
        }

        #region Static

        static StoreAtAddressInstruction()
        {
            opCodeMapping = new Dictionary<IType, OpCode>()
            {
                { PrimitiveTypes.Int8, OpCodes.StoreAddressInt8 },
                { PrimitiveTypes.Int16, OpCodes.StoreAddressInt16 },
                { PrimitiveTypes.Int32, OpCodes.StoreAddressInt32 },
                { PrimitiveTypes.Int64, OpCodes.StoreAddressInt64 },
                { PrimitiveTypes.Float32, OpCodes.StoreAddressFloat32 },
                { PrimitiveTypes.Float64, OpCodes.StoreAddressFloat64 }
            };
        }

        private static Dictionary<IType, OpCode> opCodeMapping;

        #endregion

        public IInstruction Target { get; private set; }
        public IInstruction Value { get; private set; }

        protected override OpCode GetOpCode(IType StackType)
        {
            var elemType = StackType.AsContainerType().GetElementType();
            if (opCodeMapping.ContainsKey(elemType))
            {
                return opCodeMapping[elemType];
            }
            else if (elemType.get_IsPointer())
            {
                return OpCodes.StoreAddressPointer;
            }
            else if (elemType.get_IsRootType())
            {
                return OpCodes.StoreAddressReference;
            }
            else
            {
                return OpCodes.StoreObject;
            }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Target.Emit(Context, TypeStack);
            Value.Emit(Context, TypeStack);
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
