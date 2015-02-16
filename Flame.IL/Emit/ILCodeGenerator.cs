using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILCodeGenerator : IBranchingCodeGenerator, IUnmanagedCodeGenerator
    {
        public ILCodeGenerator(IMethod Method)
        {
            this.Method = Method;
            this.variablePool = new List<IUnmanagedVariable>();
        }

        public IMethod Method { get; private set; }

        #region Block Generators

        public IBlockGenerator CreateBlock()
        {
            return new ILBlockGenerator(this);
        }

        public IIfElseBlockGenerator CreateIfElseBlock(ICodeBlock Condition)
        {
            return new ILIfElseBlock(this, Condition);
        }

        public IFlowBlockGenerator CreateWhileBlock(ICodeBlock Condition)
        {
            return new ILWhileBlockGenerator(this, Condition);
        }

        #endregion

        #region Binary Math

        public ICodeBlock EmitAdd(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.Add, A, B);
        }

        public ICodeBlock EmitAnd(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.And, A, B);
        }

        public ICodeBlock EmitDivide(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.Divide, A, B);
        }

        public ICodeBlock EmitEquals(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.CheckEquals, A, B);
        }

        public ICodeBlock EmitGreaterThan(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.CheckGreaterThan, A, B);
        }

        public ICodeBlock EmitLeftShift(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.ShiftLeft, A, B);
        }

        public ICodeBlock EmitLessThan(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.CheckLessThan, A, B);
        }

        public ICodeBlock EmitMultiply(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.Multiply, A, B);
        }

        public ICodeBlock EmitOr(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.Or, A, B);
        }

        public ICodeBlock EmitRightShift(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.ShiftRight, A, B);
        }

        public ICodeBlock EmitSubtract(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.Subtract, A, B);
        }

        public ICodeBlock EmitXor(ICodeBlock A, ICodeBlock B)
        {
            return new BinaryInstruction(this, OpCodes.Xor, A, B);
        }

        #endregion

        #region Unary Math

        public ICodeBlock EmitNot(ICodeBlock Value)
        {
            return new NotInstruction(this, Value);
        }

        #endregion

        #region Constants

        public ICodeBlock EmitBit16(ushort Value)
        {
            return new PushInt32Instruction(this, (int)(short)Value, PrimitiveTypes.Bit16);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return new PushInt32Instruction(this, (int)Value, PrimitiveTypes.Bit32);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return new PushInt64Instruction(this, (long)Value, PrimitiveTypes.Bit64);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return new PushInt32Instruction(this, (int)(sbyte)Value, PrimitiveTypes.Bit8);
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new PushInt32Instruction(this, Value ? 1 : 0, PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new PushInt32Instruction(this, (int)(short)Value, PrimitiveTypes.Char);
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return new PushInt32Instruction(this, Value, PrimitiveTypes.Int16);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new PushInt32Instruction(this, Value, PrimitiveTypes.Int32);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new PushInt64Instruction(this, Value, PrimitiveTypes.Int64);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new PushInt32Instruction(this, Value, PrimitiveTypes.Int8);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new PushInt32Instruction(this, (int)(short)Value, PrimitiveTypes.UInt16);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new PushInt32Instruction(this, (int)Value, PrimitiveTypes.UInt32);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new PushInt64Instruction(this, (long)Value, PrimitiveTypes.UInt64);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new PushInt32Instruction(this, (int)(sbyte)Value, PrimitiveTypes.UInt8);
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new PushFloat32Instruction(this, Value);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new PushFloat64Instruction(this, Value);
        }

        public ICodeBlock EmitNull()
        {
            return new PushNullInstruction(this);
        }

        public ICodeBlock EmitString(string Value)
        {
            return new PushStringInstruction(this, Value);
        }

        #endregion

        #region Object Model

        #region Conversions

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            return new TypeCastInstruction(this, Value, Type);
        }

        #endregion

        #region EmitPushPointer

        /// <summary>
        /// Emits the appropriate commands to push a pointer to the value on top of the stack.
        /// </summary>
        /// <param name="ElementType"></param>
        protected ICodeBlock EmitPushPointer(ICodeBlock Value, IType ElementType)
        {
            var block = CreateBlock();
            block.EmitBlock(Value);
            block.EmitBlock(new PushPointerInstruction(this));
            return block;
        }

        #endregion

        #region EmitIsOfType

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            var instOf = new AsInstanceOfInstruction(this, Value, Type);
            var nullBlock = EmitNull();
            return new BinaryInstruction(this, OpCodes.CheckGreaterThanUnsigned, instOf, nullBlock);
        }

        #endregion

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            return new DefaultValueInstruction(this, Type);
        }

        public ICodeBlock EmitGetField(IField Field, ICodeBlock Target)
        {
            return new FieldGetInstruction(this, Field, Target);
        }

        public ICodeBlock EmitInvocation(IMethod Method, ICodeBlock Caller, IEnumerable<ICodeBlock> Arguments)
        {
            var args = Arguments.Cast<IInstruction>().ToArray();
            if (Method.IsConstructor && Caller == null)
            {
                return new NewObjectInstruction(this, Method, args);
            }
            else
            {
                if (Method is IAccessor && Method.DeclaringType.get_IsArray())
                {
                    var property = ((IAccessor)Method).DeclaringProperty;
                    if (property.Name == "Length")
                    {
                        var block = CreateBlock();
                        block.EmitBlock(Caller);
                        block.EmitBlock(new GetArrayLengthInstruction(this));
                        return block;
                    }
                }
                return new InvocationInstruction(this, Method, (IInstruction)Caller, args);
            }
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            var args = Dimensions.Cast<IInstruction>().ToArray();
            return new NewArrayInstruction(this, ElementType, args);
        }

        public ICodeBlock EmitNewVector(IType ElementType, int[] Dimensions)
        {
            return new NewVectorInstruction(this, ElementType, Dimensions);
        }

        #endregion

        #region Branching

        public ILabel CreateLabel()
        {
            return new ILLabel(this);
        }

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            return new DereferencePointerInstruction(this, Pointer);
        }

        public ICodeBlock EmitGetFieldAddress(IField Field, ICodeBlock Target)
        {
            return new FieldAddressOfInstruction(this, Field, Target);
        }

        #region SizeOf

        public ICodeBlock EmitSizeOf(IType Type)
        {
            if (Type.get_IsPrimitive())
            {
                return EmitInt32(Type.GetPrimitiveSize());
            }
            else
            {
                return new SizeOfInstruction(this, Type);
            }
        }

        #endregion

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return new StoreAtAddressInstruction(this, Pointer, Value);
        }

        #endregion

        #region Variables

        #region Local Management

        private List<IUnmanagedVariable> variablePool;

        public void SendToPool(IUnmanagedVariable Variable)
        {
            variablePool.Add(Variable);
        }

        public void ClearVariablePool()
        {
            variablePool.Clear();
        }

        protected IUnmanagedVariable FetchFromPool(IType Type)
        {
            for (int i = 0; i < variablePool.Count; i++)
            {
                if (variablePool[i].Type.Equals(Type))
                {
                    var variable = variablePool[i];
                    variablePool.RemoveAt(i);
                    return variable;
                }
            }
            return null;
        }

        protected IUnmanagedVariable DeclareVariableInternal(IType Type)
        {
            return new ILLocalVariable(this, Type);
        }

        #endregion

        public IVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            throw new NotImplementedException();
        }

        public IVariable DeclareVariable(IType Type)
        {
            return DeclareUnmanagedVariable(Type);
        }

        public IVariable GetArgument(int Index)
        {
            return GetUnmanagedArgument(Index);
        }

        public IVariable GetThis()
        {
            return GetUnmanagedThis();
        }

        public IUnmanagedVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            throw new NotImplementedException();
        }

        public IUnmanagedVariable DeclareUnmanagedVariable(IType Type)
        {
            var poolVariable = FetchFromPool(Type);
            if (poolVariable != null)
            {
                return poolVariable;
            }
            else
            {
                return DeclareVariableInternal(Type);
            }
        }

        public IUnmanagedVariable GetUnmanagedArgument(int Index)
        {
            return new ILArgumentVariable(this, Index);
        }

        public IUnmanagedVariable GetUnmanagedThis()
        {
            return new ILThisVariable(this);
        }

        #endregion
    }
}
