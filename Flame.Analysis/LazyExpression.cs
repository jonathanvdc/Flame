using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class LazyExpression : IExpression
    {
        public LazyExpression(IAnalyzedExpression Expression, VariableMetrics State)
        {
            this.Expression = Expression;
            this.State = State;
        }

        public IAnalyzedExpression Expression { get; private set; }
        public VariableMetrics State { get; private set; }

        private IExpression cachedExpr;
        public IExpression ResultExpression
        {
            get
            {
                if (cachedExpr == null)
                {
                    cachedExpr = Expression.ToExpression(State);
                }
                return cachedExpr;
            }
        }

        public ICodeBlock Emit(ICodeGenerator Generator)
        {
            return ResultExpression.Emit(Generator);
        }

        public IBoundObject Evaluate()
        {
            return ResultExpression.Evaluate();
        }

        public bool IsConstant
        {
            get { return ResultExpression.IsConstant; }
        }

        public IExpression Optimize()
        {
            return ResultExpression.Optimize();
        }

        public IType Type
        {
            get { return ResultExpression.Type; }
        }
    }
}
