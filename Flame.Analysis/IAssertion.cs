using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    /// <summary>
    /// Describes an assertion that facilitates the elimination of redundant checks.
    /// </summary>
    public interface IAssertion
    {
        bool IsApplicable(IAnalyzedBlock Block, VariableMetrics State);
        IAnalyzedBlock Apply(IAnalyzedBlock Block, VariableMetrics State);

        /// <summary>
        /// Creates a new assertion that is logical and operation applied to this assertion and the given assertion.
        /// </summary>
        /// <param name="Other"></param>
        /// <returns></returns>
        IAssertion And(IAssertion Other);
        /// <summary>
        /// Creates a new assertion that is the logical or operation applied to this assertion.
        /// </summary>
        /// <returns></returns>
        IAssertion Not();
        /// <summary>
        /// Creates a new assertion that is logical or operation applied to this assertion and the given assertion.
        /// </summary>
        /// <param name="Other"></param>
        /// <returns></returns>
        IAssertion Or(IAssertion Other);
    }

    public static class AssertionExtensions
    {
        public static IAssertion And(this IAssertion Assertion, IEnumerable<IAssertion> Others)
        {
            return Others.Aggregate(Assertion, (a, b) => a.And(b));
        }
        public static IAssertion And(this IEnumerable<IAssertion> Assertions, IAssertion Other)
        {
            return Other.And(Assertions);
        }
        public static IAssertion And(this IEnumerable<IAssertion> Assertions)
        {
            if (Assertions.Any())
            {
                return Assertions.First().And(Assertions.Skip(1));
            }
            else
            {
                return new EmptyAssertion();
            }
        }
        public static IAssertion And(this IEnumerable<IAssertion> Assertions, IEnumerable<IAssertion> Others)
        {
            return Assertions.And().And(Others);
        }

        public static IAssertion Or(this IAssertion Assertion, IEnumerable<IAssertion> Others)
        {
            return Others.Aggregate(Assertion, (a, b) => a.Or(b));
        }
        public static IAssertion Or(this IEnumerable<IAssertion> Assertions, IAssertion Other)
        {
            return Other.Or(Assertions);
        }
        public static IAssertion Or(this IEnumerable<IAssertion> Assertions)
        {
            if (Assertions.Any())
            {
                return Assertions.First().Or(Assertions.Skip(1));
            }
            else
            {
                return new EmptyAssertion();
            }
        }
        public static IAssertion Or(this IEnumerable<IAssertion> Assertions, IEnumerable<IAssertion> Others)
        {
            return Assertions.Or().Or(Others);
        }
    }
}
