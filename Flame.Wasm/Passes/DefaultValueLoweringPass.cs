using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Native;
using Flame.Compiler.Statements;
using Flame.Compiler.Visitors;
using Flame.Compiler.Variables;

namespace Flame.Wasm.Passes
{
    /// <summary>
    /// A pass that lowers default-value expressions to field assignments.
    /// </summary>
    public class DefaultValueLoweringPass : NodeVisitorBase, IPass<IStatement, IStatement>
    {
        private DefaultValueLoweringPass()
        { }

        public static readonly DefaultValueLoweringPass Instance = new DefaultValueLoweringPass();

        /// <summary>
        /// The name of the default value lowering pass.
        /// </summary>
        public const string DefaultValueLoweringPassName = "lower-default-value";

        /// <summary>
        /// Creates a default-initialized temporary of the given type,
        /// returns its value, and releases it. This behavior is applied to
        /// all 
        /// </summary>
        public static IExpression CreateDefaultInitializedTemporary(IType Type)
        {
            var temp = new LocalVariable(Type);
            return new InitializedExpression(
                DefaultValueLoweringPass.DefaultInitialize(temp), 
                temp.CreateGetExpression(), 
                temp.CreateReleaseStatement()).Simplify();
        }

        /// <summary>
        /// Default-initializes the given variable, which is
        /// of the given type.
        /// </summary>
        public static IStatement DefaultInitialize(IUnmanagedVariable Variable, IType Type)
        {
            if (Type.GetIsPrimitive())
            {
                return Variable.CreateSetStatement(new DefaultValueExpression(Type).Optimize());
            }
            else if (Type.GetIsReferenceType())
            {
                return Variable.CreateSetStatement(new ReinterpretCastExpression(NullExpression.Instance, Type));
            }
            else
            {
                var results = new List<IStatement>();
                foreach (var field in Type.Fields)
                {
                    if (!field.IsStatic)
                    {
                        results.Add(DefaultInitialize(
                            new FieldVariable(field, Variable.CreateAddressOfExpression()), 
                            field.FieldType));
                    }
                }
                return new BlockStatement(results).Simplify();
            }
        }

        /// <summary>
        /// Default-initializes the given variable.
        /// </summary>
        public static IStatement DefaultInitialize(IUnmanagedVariable Variable)
        {
            return DefaultInitialize(Variable, Variable.Type);
        }

        public override bool Matches(IExpression Value)
        {
            return Value is DefaultValueExpression;
        }

        public override bool Matches(IStatement Value)
        {
            return Value is ISetVariableNode;
        }

        protected override IExpression Transform(IExpression Expression)
        {
            var optExpr = Expression.Optimize();
            if (!(optExpr is DefaultValueExpression))
                return Visit(optExpr);
            
            var defaultExpr = (DefaultValueExpression)optExpr;

            if (defaultExpr.Type.GetIsReferenceType())
                return NullExpression.Instance;

            return Visit(CreateDefaultInitializedTemporary(defaultExpr.Type));
        }

        protected override IStatement Transform(IStatement Statement)
        {
            var setVarNode = (ISetVariableNode)Statement;
            var destVar = setVarNode.GetVariable();
            if (destVar is IUnmanagedVariable &&
                setVarNode.Value.GetEssentialExpression() is DefaultValueExpression)
            {
                return Visit(DefaultInitialize((IUnmanagedVariable)destVar));
            }
            else
            {
                return Statement.Accept(this);
            }
        }

        public IStatement Apply(IStatement Statement)
        {
            return Visit(Statement);
        }
    }
}

