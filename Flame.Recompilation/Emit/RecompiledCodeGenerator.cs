using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class RecompiledCodeGenerator : IUnmanagedCodeGenerator, IYieldCodeGenerator, IInitializingCodeGenerator, IForeachCodeGenerator
    {
        public RecompiledCodeGenerator(AssemblyRecompiler Recompiler, IMethod Method)
        {
            this.Recompiler = Recompiler;
            this.Method = Method;
        }

        public AssemblyRecompiler Recompiler { get; private set; }
        public IMethod Method { get; private set; }

        #region GetExpression

        public static IExpression GetExpression(ICodeBlock CodeBlock)
        {
            var exprBlock = CodeBlock as ExpressionBlock;
            if (exprBlock == null)
            {
                if (CodeBlock == null)
                {
                    return null;
                }
                else if (CodeBlock is IStatementBlock)
                {
                    var types = ((IStatementBlock)CodeBlock).ResultTypes;
                    var singleElem = types.Single();
                    return new StatementExpression(GetStatement(CodeBlock), singleElem);
                }
                throw new InvalidOperationException();
            }
            else
            {
                return exprBlock.Expression;
            }
        }

        public static IEnumerable<IExpression> GetExpressions(IEnumerable<ICodeBlock> CodeBlocks)
        {
            return CodeBlocks.Select(GetExpression);
        }

        #endregion

        #region GetStatement

        public static IStatement GetStatement(ICodeBlock CodeBlock)
        {
            var stmtBlock = CodeBlock as IStatementBlock;
            if (stmtBlock == null)
            {
                throw new InvalidOperationException();
            }
            else
            {
                return stmtBlock.GetStatement();
            }
        }

        public static IEnumerable<IStatement> GetStatements(IEnumerable<ICodeBlock> CodeBlocks)
        {
            return CodeBlocks.Select(GetStatement);
        }

        #endregion

        #region Blocks

        public IBlockGenerator CreateBlock()
        {
            return new RecompiledBlockGenerator(this);
        }

        public IBlockGenerator CreateDoWhileBlock(ICodeBlock Condition)
        {
            return new DoWhileBlockGenerator(this, GetExpression(Condition));
        }

        public IIfElseBlockGenerator CreateIfElseBlock(ICodeBlock Condition)
        {
            return new IfElseBlockGenerator(this, GetExpression(Condition), CreateBlock(), CreateBlock());
        }

        public IBlockGenerator CreateWhileBlock(ICodeBlock Condition)
        {
            return new WhileBlockGenerator(this, GetExpression(Condition));
        }

        #endregion

        #region Constants

        public ICodeBlock EmitNull()
        {
            return new ExpressionBlock(this, new NullExpression());
        }

        public ICodeBlock EmitString(string Value)
        {
            return new ExpressionBlock(this, new StringExpression(Value));
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new ExpressionBlock(this, new UInt16Expression(Value));
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new ExpressionBlock(this, new UInt32Expression(Value));
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new ExpressionBlock(this, new UInt64Expression(Value));
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new ExpressionBlock(this, new UInt8Expression(Value));
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return new ExpressionBlock(this, new Bit16Expression(Value));
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return new ExpressionBlock(this, new Bit32Expression(Value));
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return new ExpressionBlock(this, new Bit64Expression(Value));
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return new ExpressionBlock(this, new Bit8Expression(Value));
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new ExpressionBlock(this, new BooleanExpression(Value));
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new ExpressionBlock(this, new CharExpression(Value));
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new ExpressionBlock(this, new Float32Expression(Value));
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new ExpressionBlock(this, new Float64Expression(Value));
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return new ExpressionBlock(this, new Int16Expression(Value));
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new ExpressionBlock(this, new Int32Expression(Value));
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new ExpressionBlock(this, new Int64Expression(Value));
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new ExpressionBlock(this, new Int8Expression(Value));
        }

        #endregion

        #region Math

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            var exprA = GetExpression(A);
            var exprB = GetExpression(B);
            var overload = Op.GetOperatorOverload(new IType[] { exprA.Type, exprB.Type });
            if (overload != null)
            {
                Recompiler.GetMethod(overload);
            }
            return new ExpressionBlock(this, new DirectBinaryExpression(exprA, Op, exprB));
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            var expr = GetExpression(Value);
            var overload = Op.GetOperatorOverload(new IType[] { expr.Type });
            if (overload != null)
            {
                Recompiler.GetMethod(overload);
            }
            return new ExpressionBlock(this, new UnaryExpression(expr, Op));
        }

        #endregion

        #region Object Model

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            return new ExpressionBlock(this, new ConversionExpression(GetExpression(Value), Recompiler.GetType(Type)));
        }

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            return new ExpressionBlock(this, new DefaultValueExpression(Recompiler.GetType(Type)));
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new ExpressionBlock(this, new InvocationExpression(GetExpression(Method), GetExpressions(Arguments)));
        }

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            return new ExpressionBlock(this, new IsExpression(GetExpression(Value), Recompiler.GetType(Type)));
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller)
        {
            return new ExpressionBlock(this, new GetMethodExpression(Recompiler.GetMethod(Method), GetExpression(Caller)));
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            return new ExpressionBlock(this, new NewArrayExpression(Recompiler.GetType(ElementType), GetExpressions(Dimensions)));
        }

        public ICodeBlock EmitNewVector(IType ElementType, int[] Dimensions)
        {
            return new ExpressionBlock(this, new NewVectorExpression(Recompiler.GetType(ElementType), Dimensions));
        }

        #endregion

        #region Variables

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
            return GetUnmanagedArgument(Index);
        }

        public IVariable GetThis()
        {
            return GetUnmanagedThis();
        }

        public IUnmanagedVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new RecompiledVariable(this, new ElementVariable(GetExpression(Value), GetExpressions(Index)));
        }

        public IUnmanagedVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            return new RecompiledVariable(this, new FieldVariable(Recompiler.GetField(Field), GetExpression(Target)));
        }

        public IUnmanagedVariable DeclareUnmanagedVariable(IVariableMember VariableMember)
        {
            return new RecompiledVariable(this, new LateBoundVariable(new RecompiledVariableMember(Recompiler, VariableMember)));
        }

        public IUnmanagedVariable GetUnmanagedArgument(int Index)
        {
            return new RecompiledVariable(this, new ArgumentVariable(Method.GetParameters()[Index], Index));
        }

        public IUnmanagedVariable GetUnmanagedThis()
        {
            return new RecompiledVariable(this, new ThisVariable(Method.DeclaringType));
        }

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            var expr = GetExpression(Pointer);
            return new ExpressionBlock(this, new DereferencePointerExpression(expr));
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return new ExpressionBlock(this, new SizeOfExpression(Recompiler.GetType(Type)));
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return new StatementBlock(this, new AtAddressVariable(GetExpression(Pointer)).CreateSetStatement(GetExpression(Value)));
        }

        #endregion

        #region IYieldCodeGenerator Implementation

        public ICodeBlock EmitYieldBreak()
        {
            return new StatementBlock(this, new YieldBreakStatement());
        }

        public ICodeBlock EmitYieldReturn(ICodeBlock Value)
        {
            return new StatementBlock(this, new YieldReturnStatement(GetExpression(Value)));
        }

        public ICodeBlock EmitInitializedArray(IType ElementType, ICodeBlock[] Items)
        {
            return new ExpressionBlock(this, new InitializedArrayExpression(Recompiler.GetType(ElementType), GetExpressions(Items).ToArray()));
        }

        public ICodeBlock EmitInitializedVector(IType ElementType, ICodeBlock[] Items)
        {
            return new ExpressionBlock(this, new InitializedVectorExpression(Recompiler.GetType(ElementType), GetExpressions(Items).ToArray()));
        }

        #endregion

        #region IForeachCodeGenerator

        public ICollectionBlock CreateCollectionBlock(IVariableMember Member, ICodeBlock Collection)
        {
            return new CollectionBlock(this, new RecompiledVariableMember(Recompiler, Member), GetExpression(Collection));
        }

        public IForeachBlockGenerator CreateForeachBlock(IEnumerable<ICollectionBlock> Collections)
        {
            return new ForeachBlockGenerator(this, Collections.Cast<CollectionBlock>());
        }

        #endregion
    }
}
