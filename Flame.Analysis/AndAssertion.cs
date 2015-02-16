using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AndAssertion : IAssertion
    {
        public AndAssertion(params IAssertion[] Assertions)
            : this((IEnumerable<IAssertion>)Assertions)
        {

        }
        public AndAssertion(IEnumerable<IAssertion> Assertions)
        {
            this.Assertions = Assertions;
        }

        public IEnumerable<IAssertion> Assertions { get; private set; }

        public bool IsApplicable(IAnalyzedBlock Block, VariableMetrics State)
        {
            return Assertions.Any((item) => item.IsApplicable(Block, State));
        }

        public IAnalyzedBlock Apply(IAnalyzedBlock Block, VariableMetrics State)
        {
            IAssertion applicable;
            IAnalyzedBlock result = Block;
            while ((applicable = Assertions.First((item) => item.IsApplicable(Block, State))) != null)
            {
                result = applicable.Apply(result, State);
            }
            return result;
        }

        public IAssertion And(IAssertion Other)
        {
            if (Other is AndAssertion)
            {
                return new AndAssertion(this.Assertions.Concat(((AndAssertion)Other).Assertions));
            }
            else
            {
                return And(new AndAssertion(Other));
            }
        }

        public IAssertion Not()
        {
            return new OrAssertion(Assertions.Select((item) => item.Not()));
        }

        public IAssertion Or(IAssertion Other)
        {
            return new OrAssertion(this, Other);
        }
    }
}
