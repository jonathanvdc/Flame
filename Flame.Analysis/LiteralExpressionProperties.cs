using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class LiteralExpressionProperties : IExpressionProperties
    {
        public LiteralExpressionProperties(IType Type)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public bool IsVolatile
        {
            get { return false; }
        }

        public bool Inline
        {
            get { return true; }
        }
    }
}
