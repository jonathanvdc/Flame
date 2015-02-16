using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class NotAssertion : IAssertion
    {
        public NotAssertion(IAssertion Assertion)
        {
            this.Assertion = Assertion;
        }

        public IAssertion Assertion { get; private set; }

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
            return new AndAssertion(this, Other);
        }

        public IAssertion Not()
        {
            return Assertion;
        }

        public IAssertion Or(IAssertion Other)
        {
            return new OrAssertion(this, Other);
        }
    }
}
