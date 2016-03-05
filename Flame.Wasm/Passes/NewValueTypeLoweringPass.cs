using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Native;
using Flame.Compiler.Statements;
using Flame.Compiler.Visitors;
using Flame.Compiler.Variables;

namespace Flame.Wasm
{
    /// <summary>
    /// A pass that lowers value type creation expressions
    /// (i.e. calls to value type constructors that do not have
    /// a 'this' pointer) to temporaries and direct calls.
    /// </summary>
    public sealed class NewValueTypeLoweringPass : NodeVisitorBase, IPass<IStatement, IStatement>
    {
        private NewValueTypeLoweringPass()
        { }

        /// <summary>
        /// The name of the new-value type expression lowering pass.
        /// </summary>
        public const string NewValueTypeLoweringPassName = "lower-new-struct";

        /// <summary>
        /// This pass' sole instance.
        /// </summary>
        public static readonly NewValueTypeLoweringPass Instance = new NewValueTypeLoweringPass();

        public override bool Matches(IExpression Value)
        {
            return Value is InvocationExpression;
        }

        public override bool Matches(IStatement Value)
        {
            return Value is ISetVariableNode;
        }

        /// <summary>
        /// Initializes the given value type variable.
        /// The given constructor is called with the given
        /// arguments.
        /// </summary>
        public static IStatement InitializeValueType(
            IUnmanagedVariable Variable, IMethod Constructor, 
            IEnumerable<IExpression> Arguments)
        {
            var result = new List<IStatement>();
            if (!Constructor.GetIsConstant())
                result.Add(Variable.CreateSetStatement(
                    new DefaultValueExpression(Variable.Type)));
            result.Add(new ExpressionStatement(new InvocationExpression(
                Constructor, 
                Variable.CreateAddressOfExpression(), 
                Arguments)));
            return new BlockStatement(result).Simplify();
        }

        /// <summary>
        /// Returns a constructor-expression pair that describes 
        /// how the given expression creates a new value type.
        /// If the given expression does not create a value type,
        /// then null is returned.
        /// </summary>
        private static Tuple<IMethod, IEnumerable<IExpression>> ExtractNewValueTypeExpr(IExpression Expression)
        {
            var invExpr = Expression as InvocationExpression;
            if (invExpr == null)
                return null;

            var target = invExpr.Target.GetEssentialExpression() as GetMethodExpression;
            if (target != null && 
                target.Caller == null && 
                target.Target.IsConstructor &&
                target.Target.DeclaringType.GetIsValueType())
            {
                return Tuple.Create(target.Target, invExpr.Arguments);
            }
            else
            {
                return null;
            }
        }

        protected override IExpression Transform(IExpression Expression)
        {
            var exprTuple = ExtractNewValueTypeExpr(Expression);

            if (exprTuple == null)
                return Expression.Accept(this);

            var temp = new LocalVariable(exprTuple.Item1.DeclaringType);
            return new InitializedExpression(
                Visit(InitializeValueType(temp, exprTuple.Item1, exprTuple.Item2)), 
                temp.CreateGetExpression(), 
                temp.CreateReleaseStatement());
        }

        protected override IStatement Transform(IStatement Statement)
        {
            var setVarNode = (ISetVariableNode)Statement;

            var exprTuple = ExtractNewValueTypeExpr(setVarNode.Value.GetEssentialExpression());
            if (exprTuple == null)
                return Statement.Accept(this);

            var destVar = setVarNode.GetVariable() as IUnmanagedVariable;
            if (destVar == null)
                return Statement.Accept(this);

            return Visit(InitializeValueType(
                destVar, exprTuple.Item1, exprTuple.Item2));
        }

        public IStatement Apply(IStatement Statement)
        {
            return Visit(Statement);
        }
    }
}

