using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    /// <summary>
    /// Describes an analyzed flow control structure.
    /// </summary>
    public class AnalyzedFlow
    {
        public AnalyzedFlow()
        {
            this.Assertion = new EmptyAssertion();
        }
        public AnalyzedFlow(IAssertion Assertion)
        {
            this.Assertion = Assertion;
        }
        public AnalyzedFlow(IAssertion Assertion, AnalyzedFlow Parent)
        {
            this.Assertion = Assertion;
            this.Parent = Parent;
        }

        /// <summary>
        /// Gets the assertion associated with this flow control structure.
        /// </summary>
        public IAssertion Assertion { get; private set; }

        private IAssertion aggregateAssertion;
        /// <summary>
        /// Gets the combined assertion of this flow control structure's assertion and that of its parents.
        /// </summary>
        public IAssertion CombinedAssertion
        {
            get
            {
                if (aggregateAssertion == null)
                {
                    if (IsRoot)
                    {
                        aggregateAssertion = Assertion;
                    }
                    else
                    {
                        aggregateAssertion = Parent.Assertion.And(Assertion);
                    }
                }
                return aggregateAssertion;
            }
        }

        /// <summary>
        /// Gets the flow control structure's parent structure.
        /// </summary>
        public AnalyzedFlow Parent { get; private set; }

        /// <summary>
        /// Gets a boolean value that indicates if this flow control structure is the root structure, i.e. has no parent.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return Parent == null;
            }
        }
    }
}
