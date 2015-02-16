using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedIfElseProperties : IStatementProperties
    {
        public AnalyzedIfElseProperties(IExpressionProperties ConditionProperties, IStatementProperties LeftProperties, IStatementProperties RightProperties)
        {
            this.ConditionProperties = ConditionProperties;
            this.LeftProperties = LeftProperties;
            this.RightProperties = RightProperties;
        }

        public IExpressionProperties ConditionProperties { get; private set; }
        public IStatementProperties LeftProperties { get; private set; }
        public IStatementProperties RightProperties { get; private set; }

        public bool IsVolatile
        {
            get { return ConditionProperties.IsVolatile || LeftProperties.IsVolatile || RightProperties.IsVolatile; }
        }
    }
}
