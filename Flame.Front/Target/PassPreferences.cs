using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    /// <summary>
    /// A data structure that stores information related to some pass.
    /// Passes with equal names are assumed to be equal.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public struct PassInfo<TIn, TOut> : IEquatable<PassInfo<TIn, TOut>>
    {
        public PassInfo(IPass<TIn, TOut> Pass, string Name)
        {
            this = default(PassInfo<TIn, TOut>);
            this.Pass = Pass;
            this.Name = Name;
        }

        /// <summary>
        /// Gets the pass this pass info structure describes.
        /// </summary>
        public IPass<TIn, TOut> Pass { get; private set; }
        /// <summary>
        /// Gets the pass' name.
        /// </summary>
        public string Name { get; private set; }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is PassInfo<TIn, TOut> && Equals((PassInfo<TIn, TOut>)obj);
        }

        public bool Equals(PassInfo<TIn, TOut> other)
        {
            return Name == other.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Defines a data structure that captures a sufficient condition for 
    /// a pass to be run.
    /// </summary>
    public struct PassCondition
    {
        /// <summary>
        /// Creates a new pass condition from the given
        /// pass name and condition.
        /// </summary>
        /// <param name="PassName"></param>
        /// <param name="Condition"></param>
        public PassCondition(string PassName, Func<OptimizationInfo, bool> Condition)
        {
            this = default(PassCondition);
            this.PassName = PassName;
            this.Condition = Condition;
        }

        /// <summary>
        /// Gets the pass name this instance is a 
        /// condition for.
        /// </summary>
        public string PassName { get; private set; }

        /// <summary>
        /// Gets a sufficient condition for the pass
        /// with this instance's name to be run.
        /// </summary>
        /// <remarks>
        /// This function takes optimization info,
        /// and returns a boolean that
        /// tells us whether the condition 
        /// has been satisfied or not.
        /// </remarks>
        public Func<OptimizationInfo, bool> Condition { get; private set; }
    }

    /// <summary>
    /// A component's list of pass preferences.
    /// </summary>
    public class PassPreferences
    {
        public PassPreferences()
            : this(Enumerable.Empty<PassCondition>())
        {
        }
        public PassPreferences(params PassCondition[] PreferredPasses)
            : this((IEnumerable<PassCondition>)PreferredPasses)
        {
        }
        public PassPreferences(IEnumerable<PassCondition> AdditionalConditions)
            : this(AdditionalConditions,
                   Enumerable.Empty<PassInfo<BodyPassArgument, IStatement>>())
        {
        }
        public PassPreferences(IEnumerable<PassCondition> AdditionalConditions,
                               IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalPasses)
        {
            this.AdditionalConditions = AdditionalConditions;
            this.AdditionalPasses = AdditionalPasses;
        }
        public PassPreferences(IEnumerable<PassCondition> AdditionalConditions,
                               IEnumerable<PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>> AdditionalAnalysisPasses)
            : this(AdditionalConditions, 
                   AdditionalAnalysisPasses.Select(BodyAnalysisPass.ToBodyPass).ToArray())
        {
        }
        public PassPreferences(IEnumerable<PassCondition> AdditionalConditions,
                               IEnumerable<PassInfo<IStatement, IStatement>> AdditionalStatementPasses)
            : this(AdditionalConditions,
                   AdditionalStatementPasses.Select(BodyStatementPass.ToBodyPass).ToArray())
        {
        }

        public IEnumerable<PassCondition> AdditionalConditions { get; private set; }
        public IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalPasses { get; private set; }

        public PassPreferences Union(PassPreferences Other)
        {
            return new PassPreferences(AdditionalConditions.Concat(Other.AdditionalConditions),
                AdditionalPasses.Union(Other.AdditionalPasses));
        }
    }
}
