using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class IntrinsicStatementProperties : IStatementProperties
    {
        public bool IsVolatile
        {
            get { return false; }
        }
    }
}
