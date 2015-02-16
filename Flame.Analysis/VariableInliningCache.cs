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
    /// <summary>
    /// A class that deals with the inlining of variables.
    /// </summary>
    public class VariableInliningCache
    {
        public VariableInliningCache(AnalyzedVariableBase AnalyzedVariable)
        {
            this.AnalyzedVariable = AnalyzedVariable;
            this.IsManaged = true;
        }

        /// <summary>
        /// Gets the analyzed variable that is using this variable inlining cache.
        /// </summary>
        public AnalyzedVariableBase AnalyzedVariable { get; private set; }

        /// <summary>
        /// Gets a boolean value that indicates whether the variable is fully managed at this time.
        /// </summary>
        public bool IsManaged { get; private set; }

        /// <summary>
        /// Makes this variable unmanaged. This will make variable inlining less aggressive.
        /// </summary>
        public void MakeUnmanaged()
        {
            this.IsManaged = false;
        }

        /// <summary>
        /// Gets a boolean value that indicates if the variable is currently in a method-initialized state.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return valueExpr != null;
            }
        }

        private IAnalyzedExpression valueExpr;
        private ConditionalStatement setStatement;
        private VariableMetrics cacheState;
        private int getExprCount;
        private bool boundToVariable;

        private ManuallyBoundVariable varCache;
        private ConstantVariable constVar;
        protected IUnmanagedVariable IndirectVariable
        {
            get
            {
                if (constVar != null)
                {
                    return constVar;
                }
                if (varCache == null)
                {
                    varCache = new ManuallyBoundVariable(AnalyzedVariable.Type);
                }
                return varCache;
            }
        }
        private void BindToVariable(IVariable Variable)
        {
            if (varCache == null)
            {
                varCache = new ManuallyBoundVariable(AnalyzedVariable.Type);
            }
            varCache.BindVariable(Variable);
        }

        public void Release(VariableMetrics State)
        {
            FlushResult(State);
            this.IsManaged = true;
        }

        public IStatement CreateSetStatement(IAnalyzedExpression Value, VariableMetrics State)
        {
            FlushResult(State);

            var expr = Value.ToExpression(State);

            this.valueExpr = Value;
            this.setStatement = new ConditionalStatement(AnalyzedVariable.GetVariable(State).CreateSetStatement(expr), !AnalyzedVariable.IsLocal);

            BindToVariable(new ExpressionVariable(expr));
            this.constVar = new ConstantVariable(IndirectVariable, expr);            

            return this.setStatement;
        }

        public IExpression CreateGetExpression(VariableMetrics State)
        {
            UpdateState(State);

            this.getExprCount++;

            BindVariable(State);

            return IndirectVariable.CreateGetExpression();
        }

        public IExpression CreateAddressOfExpression()
        {
            MakeUnmanaged();
            return IndirectVariable.CreateAddressOfExpression();
        }

        protected bool BindToExpression
        {
            get
            {
                return IsInitialized && (this.getExprCount <= 1 || this.valueExpr.ExpressionProperties.Inline) && AnalyzedVariable.Properties.IsLocal && IsManaged;
            }
        }

        private void BindVariable(VariableMetrics State)
        {
            if (this.varCache != null && !boundToVariable && !BindToExpression)
            {
                this.varCache.BindVariable(AnalyzedVariable.GetVariable(State));
                if (IsInitialized)
                {
                    this.setStatement.EmitStatement = true;
                }
                boundToVariable = true;
            }
        }

        private void FlushResult(VariableMetrics State)
        {
            this.BindVariable(State);

            this.varCache = null;
            this.constVar = null;
            this.cacheState = State;
            this.getExprCount = 0;
            this.setStatement = null;
            this.boundToVariable = false;
        }

        private void UpdateState(VariableMetrics Metrics)
        {
            if (IsInitialized && AnalyzedVariable.IsLocal)
            {
                if (Metrics.StoredSince(valueExpr.Metrics.LoadedVariables, cacheState))
                {
                    FlushResult(Metrics); // Flush on dependency
                }
            }
            else
            {
                BindToVariable(AnalyzedVariable.GetVariable(Metrics));
            }
        }
    }
}
