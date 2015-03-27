using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzingCodeGenerator : IUnmanagedCodeGenerator
    {
        public AnalyzingCodeGenerator(IMethod Method)
        {
            this.Method = Method;
            InitCache();
        }

        public IMethod Method { get; private set; }

        #region Blocks

        public IBlockGenerator CreateBlock()
        {
            return new AnalyzingBlockGenerator(this);
        }

        public IBlockGenerator CreateDoWhileBlock(ICodeBlock Condition)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock CreateIfElseBlock(ICodeBlock Condition, ICodeBlock IfBlock, ICodeBlock ElseBlock)
        {
            return new AnalyzedIfElseStatement(this, (IAnalyzedExpression)Condition, (IAnalyzedStatement)IfBlock, (IAnalyzedStatement)ElseBlock);
        }

        public IBlockGenerator CreateWhileBlock(ICodeBlock Condition)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Literals

        public ICodeBlock EmitBit16(ushort Value)
        {
            return new LiteralExpressionBlock(this, new Bit16Expression(Value));
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return new LiteralExpressionBlock(this, new Bit32Expression(Value));
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return new LiteralExpressionBlock(this, new Bit64Expression(Value));
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return new LiteralExpressionBlock(this, new Bit8Expression(Value));
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new LiteralExpressionBlock(this, new BooleanExpression(Value));
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new LiteralExpressionBlock(this, new CharExpression(Value));
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new LiteralExpressionBlock(this, new Float32Expression(Value));
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new LiteralExpressionBlock(this, new Float64Expression(Value));
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return new LiteralExpressionBlock(this, new Int16Expression(Value));
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new LiteralExpressionBlock(this, new Int32Expression(Value));
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new LiteralExpressionBlock(this, new Int64Expression(Value));
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new LiteralExpressionBlock(this, new Int8Expression(Value));
        }

        public ICodeBlock EmitString(string Value)
        {
            return new LiteralExpressionBlock(this, new StringExpression(Value));
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new LiteralExpressionBlock(this, new UInt16Expression(Value));
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new LiteralExpressionBlock(this, new UInt32Expression(Value));
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new LiteralExpressionBlock(this, new UInt64Expression(Value));
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new LiteralExpressionBlock(this, new UInt8Expression(Value));
        }

        public ICodeBlock EmitNull()
        {
            return new LiteralExpressionBlock(this, new NullExpression());
        }

        #endregion

        #region Math

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            return new BinaryExpressionBlock((IAnalyzedExpression)A, Op, (IAnalyzedExpression)B);
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            return new UnaryExpressionBlock(Op, (IAnalyzedExpression)Value);
        }

        #endregion

        #region Object Model

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            return new ConversionBlock((IAnalyzedExpression)Value, Type);
        }

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            return new LiteralExpressionBlock(this, new DefaultValueExpression(Type));
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new InvocationBlock((IAnalyzedExpression)Method, Arguments.Cast<IAnalyzedExpression>());
        }

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            return new IsOfTypeBlock((IAnalyzedExpression)Value, Type);
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller)
        {
            return new MethodDelegateBlock(this, Caller == null ? null : (IAnalyzedExpression)Caller, Method);
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            return new NewArrayBlock(this, ElementType, Dimensions.Cast<IAnalyzedExpression>());
        }

        public ICodeBlock EmitNewVector(IType ElementType, int[] Dimensions)
        {
            return new LiteralExpressionBlock(this, new NewVectorExpression(ElementType, Dimensions));
        }

        #endregion

        #region Variables

        private AnalyzedTokenVariable[] argumentVars;
        private AnalyzedVariableBase thisVar;
        private int tokenIndex;

        public AnalysisToken CreateToken()
        {
            var token = new AnalysisToken(tokenIndex);
            tokenIndex++;
            return token;
        }

        private void InitCache()
        {
            this.tokenIndex = 0;
            var parameters = Method.GetParameters();
            this.argumentVars = new AnalyzedTokenVariable[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                argumentVars[i] = new AnalyzedTokenVariable(this, new ArgumentVariable(parameters[i], i), CreateToken());
            }
            /*if (!Method.IsStatic)
            {
                var genDeclType = Method.DeclaringType;
                var declType = genDeclType.get_IsGeneric() ? genDeclType.MakeGenericType(genDeclType.GetGenericParameters()) : genDeclType;
                thisVar = new AnalyzedTokenVariable(this, new ThisVariable(declType), CreateToken());
            }*/
            thisVar = new AnalyzedThisVariable(this, Method.DeclaringType);
        }

        public IVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return GetUnmanagedElement(Value, Index);
        }

        public IVariable GetField(IField Field, ICodeBlock Target)
        {
            return GetUnmanagedField(Field, Target);
        }

        public IVariable DeclareVariable(IVariableMember VariableMember)
        {
            return DeclareUnmanagedVariable(VariableMember);
        }

        public IVariable GetArgument(int Index)
        {
            return argumentVars[Index];
        }

        public IVariable GetThis()
        {
            return thisVar;
        }

        public IUnmanagedVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new AnalyzedElementVariable(this, (IAnalyzedExpression)Value, Index.Cast<IAnalyzedExpression>());
        }

        public IUnmanagedVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            return new AnalyzedFieldVariable(this, (IAnalyzedExpression)Target, Field);
        }

        public IUnmanagedVariable DeclareUnmanagedVariable(IVariableMember VariableMember)
        {
            return new AnalyzedTokenVariable(this, new LateBoundVariable(VariableMember), CreateToken());
        }

        public IUnmanagedVariable GetUnmanagedArgument(int Index)
        {
            return argumentVars[Index];
        }

        public IUnmanagedVariable GetUnmanagedThis()
        {
            return thisVar;
        }

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            return new AnalyzedVariableGetBlock(new AnalyzedIndirectionVariable(this, (IAnalyzedExpression)Pointer));
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return new LiteralExpressionBlock(this, new SizeOfExpression(Type));
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return new AnalyzedVariableSetBlock(new AnalyzedIndirectionVariable(this, (IAnalyzedExpression)Pointer), (IAnalyzedExpression)Value);
        }

        #endregion
    }
}
