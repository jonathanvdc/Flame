using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Intermediate.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRCodeGenerator : ICodeGenerator
    {
        public IRCodeGenerator(IMethod Method)
        {
            this.Method = Method;
        }

        public IMethod Method { get; private set; }

        #region Literals

        public INodeBlock EmitLiteral(object Value, string LiteralName, IType Type)
        {
            return new PrimitiveNodeBlock(this, NodeFactory.Literal(Value), Type);
        }

        #region Bit types

        public ICodeBlock EmitBit8(byte Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit8Name, PrimitiveTypes.Bit8);
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit16Name, PrimitiveTypes.Bit16);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit32Name, PrimitiveTypes.Bit32);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBit64Name, PrimitiveTypes.Bit64);
        }

        #endregion

        #region Signed integer types

        public ICodeBlock EmitInt16(short Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt16Name, PrimitiveTypes.Int16);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt32Name, PrimitiveTypes.Int32);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt64Name, PrimitiveTypes.Int64);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantInt8Name, PrimitiveTypes.Int8);
        }

        #endregion

        #region Unsigned integer types

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt16Name, PrimitiveTypes.UInt16);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt32Name, PrimitiveTypes.UInt32);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt64Name, PrimitiveTypes.UInt64);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantUInt8Name, PrimitiveTypes.UInt8);
        }

        #endregion

        #region Floating point types

        public ICodeBlock EmitFloat32(float Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantFloat32Name, PrimitiveTypes.Float32);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantFloat64Name, PrimitiveTypes.Float64);
        }

        #endregion

        #region Miscellaneous types

        public ICodeBlock EmitBoolean(bool Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantBooleanName, PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantCharName, PrimitiveTypes.Char);
        }

        public ICodeBlock EmitString(string Value)
        {
            return EmitLiteral(Value, ExpressionParsers.ConstantStringName, PrimitiveTypes.String);
        }

        #endregion

        #region Void

        public ICodeBlock EmitVoid()
        {
            return new PrimitiveNodeBlock(this, NodeFactory.Id(ExpressionParsers.ConstantVoidName), PrimitiveTypes.Void);
        }

        #endregion

        #region Null

        public ICodeBlock EmitNull()
        {
            return new PrimitiveNodeBlock(this, NodeFactory.Id(ExpressionParsers.ConstantNullName), PrimitiveTypes.Null);
        }

        #endregion

        #endregion

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitBreak(BlockTag Target)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitContinue(BlockTag Target)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitTagged(BlockTag Tag, ICodeBlock Contents)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            throw new NotImplementedException();
        }

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            throw new NotImplementedException();
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            throw new NotImplementedException();
        }

        public IEmitVariable DeclareVariable(IVariableMember VariableMember)
        {
            throw new NotImplementedException();
        }

        public IEmitVariable GetArgument(int Index)
        {
            throw new NotImplementedException();
        }

        public IEmitVariable GetThis()
        {
            throw new NotImplementedException();
        }
    }
}
