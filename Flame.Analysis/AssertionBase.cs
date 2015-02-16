using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public abstract class AssertionBase<T> : IAssertion
        where T : IAnalyzedBlock
    {
        public abstract bool IsApplicable(T Block, VariableMetrics State);
        public abstract IAnalyzedBlock Apply(T Block, VariableMetrics State);

        public bool IsApplicable(IAnalyzedBlock Block, VariableMetrics State)
        {
            if (Block is T)
            {
                return IsApplicable((T)Block, State);
            }
            else
            {
                return false;
            }
        }

        public IAnalyzedBlock Apply(IAnalyzedBlock Block, VariableMetrics State)
        {
            return Apply((T)Block, State);
        }

        public virtual IAssertion And(IAssertion Other)
        {
            return new AndAssertion(this, Other);
        }
        public virtual IAssertion Or(IAssertion Other)
        {
            return new OrAssertion(this, Other);
        }
        public virtual IAssertion Not()
        {
            return new NotAssertion(this);
        }
    }

    public abstract class LogicalAssertionBase<T, TAssertion> : AssertionBase<T>
        where T : IAnalyzedBlock
        where TAssertion : IAssertion
    {
        public abstract IAssertion And(TAssertion Other);
        public abstract IAssertion Or(TAssertion Other);
        public abstract override IAssertion Not();

        public override IAssertion And(IAssertion Other)
        {
            if (Other is TAssertion)
            {
                return And((TAssertion)Other);
            }
            else
            {
                return base.And(Other);
            }
        }

        public override IAssertion Or(IAssertion Other)
        {
            if (Other is TAssertion)
            {
                return Or((TAssertion)Other);
            }
            else
            {
                return base.And(Other);
            }
        }
    }
}
