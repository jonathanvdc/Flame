using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    /// <summary>
    /// Describes an analyzed block that provides a number of assertions, assuming this block is true.
    /// </summary>
    public interface IAssertionBlock : IAnalyzedBlock
    {
        IAssertion GetAssertion(VariableMetrics State);
        IAnalyzedBlock ApplyAssertion(IAssertion Assertion, VariableMetrics State);
    }
}
