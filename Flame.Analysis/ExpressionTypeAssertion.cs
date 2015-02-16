using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class ExpressionTypeAssertion : LogicalAssertionBase<IsOfTypeBlock, ExpressionTypeAssertion>
    {
        public ExpressionTypeAssertion(IAnalyzedExpression Expression, IType Type, VariableMetrics AssertionState)
        {
            this.Expression = Expression;
            this.AssertionState = AssertionState;
            this.Type = Type;
        }

        public IAnalyzedExpression Expression { get; private set; }
        public VariableMetrics AssertionState { get; private set; }
        public IType Type { get; private set; }

        public override bool IsApplicable(IsOfTypeBlock Block, VariableMetrics State)
        {
            return Expression.Equals(Block.Value) && AssertionState.StoredSince(Expression.Metrics.LoadedVariables, State) && Block.Type.Is(Type);
        }

        public override IAnalyzedBlock Apply(IsOfTypeBlock Block, VariableMetrics State)
        {
            return (IAnalyzedBlock)Expression.CodeGenerator.EmitBoolean(true);
        }

        public override IAssertion And(ExpressionTypeAssertion Other)
        {
            if (Expression.Equals(Other.Expression) && !AssertionState.StoredSince(Expression.Metrics.LoadedVariables, Other.AssertionState))
            {
                if (Other.Type.Is(this.Type))
                {
                    return this;
                }
                else if (this.Type.Is(Other.Type))
                {
                    return Other;
                }
            }
            return new AndAssertion(this, Other);
        }

        public override IAssertion Or(ExpressionTypeAssertion Other)
        {
            return new OrAssertion(this, Other);
        }

        public override IAssertion Not()
        {
            return new ExpressionNotTypeAssertion(Expression, Type, AssertionState);
        }
    }
}
