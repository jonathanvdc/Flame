using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class VariableBlockProperties : IExpressionProperties, IStatementProperties
    {
        public VariableBlockProperties(IType Type, bool Inline)
        {
            this.Type = Type;
            this.Inline = Inline;
        }

        public IType Type { get; private set; }
        public bool Inline { get; private set; }

        public bool IsVolatile
        {
            get { return false; }
        }
    }
}
