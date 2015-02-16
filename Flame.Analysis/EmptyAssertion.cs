using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class EmptyAssertion : IAssertion
    {
        public bool IsApplicable(IAnalyzedBlock Block, VariableMetrics State)
        {
            return false;
        }

        public IAnalyzedBlock Apply(IAnalyzedBlock Block, VariableMetrics State)
        {
            return Block;
        }

        public IAssertion And(IAssertion Other)
        {
            // a && true == a
            return Other;
        }

        public IAssertion Not()
        {
            return this;
        }

        public IAssertion Or(IAssertion Other)
        {
            // a || true == true
            return this;
        }
    }
}
