using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    #region CacheVariableInitializationStatement

    public class CacheVariableInitializationStatement : IStatement
    {
        public CacheVariableInitializationStatement(ManuallyBoundVariable Variable, IExpression Value)
        {
            this.Variable = Variable;
            this.Value = Value;
        }

        public ManuallyBoundVariable Variable { get; private set; }
        public IExpression Value { get; private set; }

        public void Emit(IBlockGenerator Generator)
        {
            if (!Variable.IsBound)
            {
                Variable.BindVariable(new LateBoundVariable(Variable.Member));
                Variable.CreateSetStatement(Value);
            }
        }

        public bool IsEmpty
        {
            get { return false; }
        }

        public IStatement Optimize()
        {
            return new CacheVariableInitializationStatement(Variable, Value);
        }
    }

    #endregion

    #region DuplicateExpressionCache

    public class DuplicateExpressionCache
    {
        public DuplicateExpressionCache(IAnalyzedExpression Expression)
        {
            this.Expression = Expression;
        }

        public IAnalyzedExpression Expression { get; private set; }

        private VariableMetrics cacheState;
        private int exprOccurence;
        private ManuallyBoundVariable cache;
        public IVariable Result
        {
            get
            {
                if (cache == null)
                {
                    cache = new ManuallyBoundVariable(Expression.ExpressionProperties.Type);
                }
                return this.cache;
            }
        }

        public IExpression ToExpression(VariableMetrics State)
        {
            UpdateState(State);

            this.exprOccurence++;
            var result = (ManuallyBoundVariable)Result;
            var getExpr = result.CreateGetExpression();
            if (this.exprOccurence == 1)
            {
                var initStatement = new CacheVariableInitializationStatement(result, Expression.ToExpression(State));
                return new InitializedExpression(initStatement, getExpr);
            }
            return getExpr;
        }

        public void FlushResult(VariableMetrics State)
        {
            if (this.exprOccurence <= 1)
            {
                this.cache.BindVariable(new ExpressionVariable(Expression.ToExpression(State)));
            }
            this.cache = null;
            this.cacheState = State;
            this.exprOccurence = 0;
        }

        private void UpdateState(VariableMetrics Metrics)
        {
            if (Metrics.StoredSince(Expression.Metrics.LoadedVariables, cacheState))
            {
                FlushResult(Metrics); // Flush on dependency
            }
        }
    }

    #endregion

    #region DuplicateExpressionBlock

    public class DuplicateExpressionBlock : IAnalyzedExpression
    {
        public DuplicateExpressionBlock(IAnalyzedExpression Expression)
        {
            this.Cache = new DuplicateExpressionCache(Expression);
        }

        public DuplicateExpressionCache Cache { get; private set; }

        public IAnalyzedExpression Expression { get { return Cache.Expression; } }

        public IExpressionProperties ExpressionProperties
        {
            get { return Expression.ExpressionProperties; }
        }

        public IExpression ToExpression(VariableMetrics State)
        {
            return Cache.ToExpression(State);
        }

        public VariableMetrics Metrics
        {
            get { return Expression.Metrics; }
        }

        public IBlockProperties Properties
        {
            get { return Expression.Properties; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Expression.CodeGenerator; }
        }

        public bool Equals(IAnalyzedBlock other)
        {
            if (other is DuplicateExpressionBlock)
            {
                return this.Expression.Equals(((DuplicateExpressionBlock)other).Expression);
            }
            else
            {
                return this.Expression.Equals(other);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is IAnalyzedBlock)
            {
                return this.Equals((IAnalyzedBlock)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Expression.GetHashCode();
        }

        public IAnalyzedStatement InitializationStatement
        {
            get { return Expression.InitializationStatement; }
        }
    }

    #endregion
}
