using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class OrAssertion : IAssertion
    {
        public OrAssertion(params IAssertion[] Assertions)
            : this((IEnumerable<IAssertion>)Assertions)
        {

        }
        public OrAssertion(IEnumerable<IAssertion> Assertions)
        {
            this.Assertions = Assertions;
        }

        public IEnumerable<IAssertion> Assertions { get; private set; }

        public bool IsApplicable(IAnalyzedBlock Block, VariableMetrics State)
        {
            return false;
        }

        public IAnalyzedBlock Apply(IAnalyzedBlock Block, VariableMetrics State)
        {
            return Block;
        }

        public IAssertion Or(IAssertion Other)
        {
            if (Other is OrAssertion)
            {
                return new OrAssertion(this.Assertions.Concat(((OrAssertion)Other).Assertions));
            }
            else
            {
                return And(new OrAssertion(Other));
            }
        }

        public IAssertion And(IAssertion Other)
        {
            return new AndAssertion(this, Other);
        }

        public IAssertion Not()
        {
            return new AndAssertion(Assertions.Select((item) => item.Not()));
        }
    }
}
