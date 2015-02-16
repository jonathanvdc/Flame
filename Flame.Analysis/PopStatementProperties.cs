using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class PopStatementProperties : IStatementProperties
    {
        public PopStatementProperties(IExpressionProperties ExpressionProperties)
        {
            this.ExpressionProperties = ExpressionProperties;
        }

        public IExpressionProperties ExpressionProperties { get; private set; }

        public bool IsVolatile
        {
            get { return ExpressionProperties.IsVolatile; }
        }
    }
}
