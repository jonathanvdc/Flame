using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public static class AnalysisExtensions
    {
        #region Analyzed Blocks and Code Metrics

        public static VariableMetrics GetMetricsOrDefault(this IAnalyzedBlock Block)
        {
            return Block == null ? new VariableMetrics() : Block.Metrics;
        }

        public static IExpression ToExpressionOrNull(this IAnalyzedExpression Block, VariableMetrics Metrics)
        {
            return Block == null ? null : Block.ToExpression(Metrics);
        }

        public static IEnumerable<IExpression> ToExpressions(this IEnumerable<IAnalyzedExpression> Blocks, VariableMetrics Metrics)
        {
            VariableMetrics state = Metrics;
            foreach (var item in Blocks)
            {
                yield return item.ToExpression(state);
                state = state.Pipe(item.Metrics);
            }
        }

        public static IEnumerable<IStatement> ToStatements(this IEnumerable<IAnalyzedStatement> Blocks, VariableMetrics Metrics)
        {
            VariableMetrics state = Metrics;
            foreach (var item in Blocks)
            {
                yield return item.ToStatement(state);
                state = state.Pipe(item.Metrics);
            }
        }

        public static VariableMetrics Pipe(this VariableMetrics State, IEnumerable<VariableMetrics> Metrics)
        {
            return Metrics.Aggregate(State, (a, b) => a.Pipe(b));
        }

        public static VariableMetrics Pipe(this IEnumerable<VariableMetrics> Metrics)
        {
            return new VariableMetrics().Pipe(Metrics);
        }

        public static VariableMetrics PipeMetrics(this VariableMetrics State, IEnumerable<IAnalyzedBlock> Blocks)
        {
            return Blocks.Aggregate(State, (a, b) => a.Pipe(b.Metrics));
        }

        public static VariableMetrics PipeMetrics(this IEnumerable<IAnalyzedBlock> Blocks)
        {
            return new VariableMetrics().PipeMetrics(Blocks);
        }

        public static VariableMetrics PipeMetrics(this VariableMetrics State, IAnalyzedBlock Block)
        {
            return State.Pipe(Block.Metrics);
        }

        public static IEnumerable<VariableMetrics> ToMetrics(this IEnumerable<IAnalyzedBlock> Blocks)
        {
            return Blocks.Select((item) => item.Metrics);
        }

        #endregion

        #region Types

        public static IEnumerable<IType> GetTypes(this IEnumerable<IExpressionProperties> Properties)
        {
            return Properties.Select((item) => item.Type);
        }

        public static IEnumerable<IType> GetTypes(this IEnumerable<IAnalyzedExpression> Expressions)
        {
            return Expressions.Select((item) => item.ExpressionProperties.Type);
        }

        public static bool IsVoid(this IType Type)
        {
            return Type == null || Type.Equals(PrimitiveTypes.Void);
        }

        #endregion

        #region GetToken

        public static IAnalyzedVariable GetReturnVariable(this IAnalyzedExpression Expression)
        {
            var metrics = Expression.Metrics;
            return metrics.Returns.SingleOrDefault();
        }

        #endregion

        #region Equality

        public static bool EqualsOrNull<T>(this IEquatable<T> Item, T Other)
        {
            return object.ReferenceEquals(Item, Other) || (Item != null && Item.Equals(Other));
        }

        #endregion

        #region Initialization Statements

        public static IAnalyzedStatement GetInitializationStatement(this IEnumerable<IAnalyzedBlock> Blocks, ICodeGenerator CodeGenerator)
        {
            return new SimpleAnalyzedBlockStatement(CodeGenerator, Blocks.Select((item) => item.InitializationStatement));
        }

        #endregion

        #region Lazy Expressions

        public static IExpression ToLazyExpression(this IAnalyzedExpression Expression, VariableMetrics State)
        {
            return new LazyExpression(Expression, State);
        }

        #endregion

        #region Assertions

        public static IAssertion GetAssertion(this IAnalyzedBlock Block, VariableMetrics State)
        {
            if (Block is IAssertionBlock)
            {
                return ((IAssertionBlock)Block).GetAssertion(State);
            }
            else
            {
                return new EmptyAssertion();
            }
        }

        public static T ApplyAssertion<T>(this T Block, IAssertion Assertion, VariableMetrics State)
            where T : IAnalyzedBlock
        {
            if (Block is IAssertionBlock)
            {
                return (T)((IAssertionBlock)Block).ApplyAssertion(Assertion, State);
            }
            else
            {
                return (T)Assertion.Apply(Block, State);
            }
        }

        #endregion
    }
}
