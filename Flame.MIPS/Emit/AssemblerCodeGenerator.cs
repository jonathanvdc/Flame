using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerCodeGenerator : ICodeGenerator, IBranchingCodeGenerator, 
                                          IUnmanagedCodeGenerator
    {
        public AssemblerCodeGenerator(IMethod Method)
        {
            this.Method = Method;
        }

        public IMethod Method { get; private set; }

        #region Branching

        public ILabel CreateLabel()
        {
            return new AssemblerLateBoundLabel(this);
        }

        public ILabel CreateLabel(string Name)
        {
            return new AssemblerLateBoundLabel(this, Name);
        }

        #endregion

        #region Blocks

        public ICodeBlock EmitBreak(UniqueTag Tag)
        {
            return new BreakBlock(this, Tag);
        }

        public ICodeBlock EmitContinue(UniqueTag Tag)
        {
            return new ContinueBlock(this, Tag);
        }

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            return new TaggedBlock(this, Tag, (IAssemblerBlock)Contents);
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            return new IfElseBlock(this, (IAssemblerBlock)Condition, (IAssemblerBlock)IfBody, (IAssemblerBlock)ElseBody);
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            return new PopBlock((IAssemblerBlock)Value);
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            return new ReturnBlock((IAssemblerBlock)Value);
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            return new SequenceBlock(this, (IAssemblerBlock)First, (IAssemblerBlock)Second);
        }

        public ICodeBlock EmitVoid()
        {
            return new EmptyBlock(this);
        }

        #endregion

        #region Math

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            var left = (IAssemblerBlock)A;
            var right = (IAssemblerBlock)B;
            if (BinaryOpBlock.IsSupported(Op, left.Type))
	        {
                if (left.Type.GetIsPointer() && right.Type.GetIsInteger())
                {
                    return new BinaryOpBlock(this, left, Op, (IAssemblerBlock)EmitBinary(right, EmitInt32(left.Type.AsContainerType().ElementType.GetSize()), Operator.Multiply));
                }
                else if (right.Type.GetIsPointer() && left.Type.GetIsInteger())
                {
                    return new BinaryOpBlock(this, (IAssemblerBlock)EmitBinary(left, EmitInt32(right.Type.AsContainerType().ElementType.GetSize()), Operator.Multiply), Op, right);
                }
                return new BinaryOpBlock(this, left, Op, right);
	        }
            else
            {
                return null;
            }
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            if (Op.Equals(Operator.Not))
            {
                if (Value is BinaryOpBlock)
                {
                    var binOp = (BinaryOpBlock)Value;
                    Operator neg;
                    if (NotBlock.TryGetNegatedOperator(binOp.Operator, out neg))
                    {
                        return new BinaryOpBlock(this, binOp.Left, neg, binOp.Right);
                    }
                }
                return new NotBlock((IAssemblerBlock)Value);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Constants

        public ICodeBlock EmitInt16(short Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Int16, Value);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Int32, Value);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Int64, Value);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Int8, Value);
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Bit16, Value);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Bit32, Value);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Bit64, (long)Value);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Bit8, Value);
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Boolean, Value ? 1 : 0);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.Char, (byte)Value);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.UInt16, Value);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.UInt32, Value);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.UInt64, (long)Value);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new ConstantBlock(this, PrimitiveTypes.UInt8, Value);
        }

        public ICodeBlock EmitNull()
        {
            return new ConstantBlock(this, PrimitiveTypes.Null, 0);
        }

        public ICodeBlock EmitString(string Value)
        {
            return new LoadStringConstantBlock(this, Value);
        }

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            if (Type.GetIsInteger() || Type.GetIsBit())
            {
                return EmitConversion(EmitInt32(0), Type);
            }
            else if (Type.Equals(PrimitiveTypes.Char))
            {
                return EmitChar(default(char));
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return EmitBoolean(false);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Conversion

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            return new ConversionBlock((IAssemblerBlock)Value, Type);
        }

        #endregion

        #region Method Calls

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new InvocationBlock(this, (IAssemblerBlock)Method, Arguments.Cast<IAssemblerBlock>());
        }

        public ICodeBlock EmitDirectMethod(IMethod Method, ICodeBlock Caller)
        {
            if (Caller != null)
            {
                throw new NotSupportedException("Function calls with non-empty callers are not supported.");
            }
            return new MethodBlock(Method, this);
        }

        #endregion

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            if (Op.Equals(Operator.GetDelegate))
            {
                return EmitDirectMethod(Method, Caller);
            }
            else
            {
                return null; // Indirect function calls are not supported.
            }
        }

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            if (Op.Equals(Operator.StaticCast) || Op.Equals(Operator.ReinterpretCast))
            {
                return EmitConversion(Value, Type);
            }
            else
            {
                return null;
            }
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            throw new NotImplementedException();
        }

        #region Variables

        #region Locals

        private Dictionary<UniqueTag, AssemblerLocalVariable> locals = new Dictionary<UniqueTag, AssemblerLocalVariable>();

        public IEmitVariable GetLocal(UniqueTag Tag)
        {
            return GetUnmanagedLocal(Tag);
        }

        public IEmitVariable DeclareLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return DeclareUnmanagedLocal(Tag, VariableMember);
        }

        public IUnmanagedEmitVariable GetUnmanagedLocal(UniqueTag Tag)
        {
            AssemblerLocalVariable result;
            if (locals.TryGetValue(Tag, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public IUnmanagedEmitVariable DeclareUnmanagedLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            var result = new AssemblerLocalVariable(VariableMember, this);
            locals.Add(Tag, result);
            return result;
        }

        #endregion

        public IEmitVariable GetArgument(int Index)
        {
            return new AssemblerArgument(this, Index);
        }

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            throw new NotImplementedException();
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            throw new NotImplementedException();
        }

        public IEmitVariable GetThis()
        {
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedArgument(int Index)
        {
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedThis()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            return new DereferenceBlock((IAssemblerBlock)Pointer);
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return EmitInt32(Type.GetSize());
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return new StoreAtBlock((IAssemblerBlock)Pointer, (IAssemblerBlock)Value);
        }

        #endregion

    }
}
