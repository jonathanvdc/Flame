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
    public class RecompiledCodeGenerator : IUnmanagedCodeGenerator, IYieldCodeGenerator,
        IInitializingCodeGenerator, IForeachCodeGenerator, IExceptionCodeGenerator,
        IForCodeGenerator, IContractCodeGenerator, IWhileCodeGenerator, IDoWhileCodeGenerator,
        ILambdaCodeGenerator
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
                var exprBlock = CodeBlock as ExpressionBlock;
                if (exprBlock != null)
                {
                    return new RawExpressionStatement(exprBlock.Expression);
                }
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

        #region GetType

        public static IEnumerable<IType> GetResultTypes(ICodeBlock Block)
        {
            if (Block is ExpressionBlock)
            {
                return new IType[] { ((ExpressionBlock)Block).Expression.Type };
            }
            return ((IStatementBlock)Block).ResultTypes;
        }

        #endregion

        #region Blocks

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            return new StatementBlock(this, new TaggedStatement(Tag, GetStatement(Contents)));
        }

        public ICodeBlock EmitBreak(UniqueTag Tag)
        {
            return new StatementBlock(this, new BreakStatement(Tag));
        }

        public ICodeBlock EmitContinue(UniqueTag Tag)
        {
            return new StatementBlock(this, new ContinueStatement(Tag));
        }

        public ICodeBlock EmitDoWhile(UniqueTag Tag, ICodeBlock Body, ICodeBlock Condition)
        {
            return new StatementBlock(this, new DoWhileStatement(Tag, GetStatement(Body), GetExpression(Condition)));
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            return new IfElseBlock(this, GetExpression(Condition), IfBody, ElseBody);
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            return new StatementBlock(this, new ExpressionStatement(GetExpression(Value)));
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            return new StatementBlock(this, new ReturnStatement(GetExpression(Value)));
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            return new SequenceBlock(this, First, Second);
        }

        public ICodeBlock EmitVoid()
        {
            return new StatementBlock(this, EmptyStatement.Instance);
        }

        public ICodeBlock EmitWhile(UniqueTag Tag, ICodeBlock Condition, ICodeBlock Body)
        {
            return new StatementBlock(this, new WhileStatement(Tag, GetExpression(Condition), GetStatement(Body)));
        }

        #endregion

        #region Constants

        public ICodeBlock EmitNull()
        {
            return new ExpressionBlock(this, NullExpression.Instance);
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

            return new ExpressionBlock(this, DirectBinaryExpression.Instance.Create(exprA, Op, exprB));
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            var expr = GetExpression(Value);
            Recompiler.GetOperatorOverload(Op, expr.Type);

            return new ExpressionBlock(this, new UnaryExpression(expr, Op));
        }

        #endregion

        #region Object Model

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            var lhs = GetExpression(Value);
            var rhs = Recompiler.GetType(Type);
            if (Op.Equals(Operator.IsInstance))
            {
                return new ExpressionBlock(this, new IsExpression(lhs, rhs));
            }
            else if (Op.Equals(Operator.AsInstance))
            {
                return new ExpressionBlock(this, new AsInstanceExpression(lhs, rhs));
            }
            else if (Op.Equals(Operator.StaticCast))
            {
                return new ExpressionBlock(this, new StaticCastExpression(lhs, rhs));
            }
            else if (Op.Equals(Operator.DynamicCast))
            {
                return new ExpressionBlock(this, new DynamicCastExpression(lhs, rhs));
            }
            else if (Op.Equals(Operator.ReinterpretCast))
            {
                return new ExpressionBlock(this, new ReinterpretCastExpression(lhs, rhs));
            }
            else
            {
                return null;
            }
        }

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            return new ExpressionBlock(this, new DefaultValueExpression(Recompiler.GetType(Type)));
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new ExpressionBlock(this, new InvocationExpression(GetExpression(Method), GetExpressions(Arguments)));
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            if (Op.Equals(Operator.GetCurriedDelegate))
            {
                return new ExpressionBlock(this, new GetExtensionMethodExpression(Recompiler.GetMethod(Method), GetExpression(Caller)));
            }
            else
            {
                return new ExpressionBlock(this, new GetMethodExpression(Recompiler.GetMethod(Method), GetExpression(Caller), Op));
            }
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            return new ExpressionBlock(this, new NewArrayExpression(Recompiler.GetType(ElementType), GetExpressions(Dimensions)));
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            return new ExpressionBlock(this, new NewVectorExpression(Recompiler.GetType(ElementType), Dimensions));
        }

        #endregion

        #region Variables

        private Dictionary<UniqueTag, RecompiledVariable> locals = new Dictionary<UniqueTag, RecompiledVariable>();

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return GetUnmanagedElement(Value, Index);
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return GetUnmanagedField(Field, Target);
        }

        public IEmitVariable GetLocal(UniqueTag Tag)
        {
            return GetUnmanagedLocal(Tag);
        }

        public IEmitVariable DeclareLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            return DeclareUnmanagedLocal(Tag, VariableMember);
        }

        public IEmitVariable GetArgument(int Index)
        {
            return GetUnmanagedArgument(Index);
        }

        public IEmitVariable GetThis()
        {
            return GetUnmanagedThis();
        }

        public IUnmanagedEmitVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new RecompiledVariable(this, new ElementVariable(GetExpression(Value), GetExpressions(Index)));
        }

        public IUnmanagedEmitVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            return new RecompiledVariable(this, new FieldVariable(Recompiler.GetField(Field), GetExpression(Target)));
        }

        public IUnmanagedEmitVariable GetUnmanagedLocal(UniqueTag Tag)
        {
            RecompiledVariable result;
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
            var result = new RecompiledVariable(this, new LocalVariable(new RetypedVariableMember(VariableMember, Recompiler.GetType(VariableMember.VariableType))));
            locals.Add(Tag, result);
            return result;
        }

        public IUnmanagedEmitVariable GetUnmanagedArgument(int Index)
        {
            return new RecompiledVariable(this, new ArgumentVariable(Method.GetParameters()[Index], Index));
        }

        public IUnmanagedEmitVariable GetUnmanagedThis()
        {
            return new RecompiledVariable(this, new ThisVariable(Method.DeclaringType));
        }

        public IEmitVariable ReturnVariable
        {
            get { return new RecompiledVariable(this, new ReturnValueVariable(Method.ReturnType)); }
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

        public ICollectionBlock EmitCollectionBlock(IVariableMember Member, ICodeBlock Collection)
        {
            return new CollectionBlock(this, new RetypedVariableMember(Member, Recompiler.GetType(Member.VariableType)), GetExpression(Collection));
        }

        public ICodeBlock EmitForeachBlock(IForeachBlockHeader Header, ICodeBlock Body)
        {
            return new StatementBlock(this, ((ForeachBlockHeader)Header).ToForeachStatement(GetStatement(Body)));
        }

        public IForeachBlockHeader EmitForeachHeader(UniqueTag Tag, IEnumerable<ICollectionBlock> Collections)
        {
            return new ForeachBlockHeader(this, Tag, Collections.Cast<CollectionBlock>());
        }

        #endregion

        #region IExceptionCodeGenerator

        public ICatchClause EmitCatchClause(ICatchHeader Header, ICodeBlock Body)
        {
            var clause = (CatchHeader)Header;
            clause.SetBody(GetStatement(Body));
            return clause;
        }

        public ICatchHeader EmitCatchHeader(IVariableMember ExceptionVariable)
        {
            return new CatchHeader(this, ExceptionVariable);
        }

        public ICodeBlock EmitTryBlock(ICodeBlock TryBody, ICodeBlock FinallyBody, IEnumerable<ICatchClause> CatchClauses)
        {
            return new StatementBlock(this, new TryStatement(GetStatement(TryBody), GetStatement(FinallyBody), CatchClauses.Select(item => ((CatchHeader)item).ToClause())));
        }

        public ICodeBlock EmitAssert(ICodeBlock Condition)
        {
            return new StatementBlock(this, new AssertStatement(GetExpression(Condition)));
        }

        public ICodeBlock EmitThrow(ICodeBlock Exception)
        {
            return new StatementBlock(this, new ThrowStatement(GetExpression(Exception)));
        }

        #endregion

        #region IForCodeGenerator

        public ICodeBlock EmitForBlock(UniqueTag Tag, ICodeBlock Initialization, ICodeBlock Condition, ICodeBlock Delta, ICodeBlock Body)
        {
            return new StatementBlock(this, new ForStatement(Tag, GetStatement(Initialization), GetExpression(Condition), GetStatement(Delta), GetStatement(Body), EmptyStatement.Instance));
        }

        #endregion

        #region Contracts

        public ICodeBlock EmitContractBlock(IEnumerable<ICodeBlock> Preconditions, IEnumerable<ICodeBlock> Postconditions, ICodeBlock Body)
        {
            return new StatementBlock(this, new ContractBodyStatement(GetStatement(Body), GetExpressions(Preconditions), GetExpressions(Postconditions)));
        }

        #endregion

        #region Lambdas

        public ICodeBlock EmitLambda(ILambdaHeaderBlock Header, ICodeBlock Body)
        {
            return new ExpressionBlock(this, ((LambdaHeaderBlock)Header).CreateLambda(GetStatement(Body)));
        }

        public ILambdaHeaderBlock EmitLambdaHeader(IMethod Member, IEnumerable<ICodeBlock> CapturedValues)
        {
            return new LambdaHeaderBlock(Recompiler, new LambdaHeader(Member, GetExpressions(CapturedValues).ToArray()));
        }

        #endregion
    }
}
