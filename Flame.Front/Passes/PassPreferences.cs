using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Recompilation;
using Flame.Front.Passes;
using Flame.Front.Target;

namespace Flame.Front.Passes
{
    using AnalysisPassInfo = PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>;
    using MethodPassInfo = PassInfo<BodyPassArgument, IStatement>;
    using SignaturePassInfo = PassInfo<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using StatementPassInfo = PassInfo<IStatement, IStatement>;
    using RootPassInfo = PassInfo<BodyPassArgument, IEnumerable<IMember>>;

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
    /// A component's set of pass preferences.
    /// </summary>
    public class PassPreferences
    {
        public PassPreferences()
            : this(Enumerable.Empty<PassCondition>())
        { }
        public PassPreferences(params PassCondition[] Conditions)
            : this((IEnumerable<PassCondition>)Conditions)
        { }
        public PassPreferences(IEnumerable<PassCondition> Conditions)
            : this(Conditions,
                   Enumerable.Empty<MethodPassInfo>())
        { }
        public PassPreferences(
			IEnumerable<PassCondition> Conditions,
            IEnumerable<MethodPassInfo> MethodPasses)
            : this(Conditions, MethodPasses, Enumerable.Empty<RootPassInfo>())
        { }
        public PassPreferences(
			IEnumerable<PassCondition> Conditions,
            IEnumerable<AnalysisPassInfo> AnalysisPasses)
            : this(Conditions, 
                   AnalysisPasses.Select(BodyAnalysisPass.ToBodyPass).ToArray())
        { }
        public PassPreferences(
			IEnumerable<PassCondition> Conditions,
            IEnumerable<StatementPassInfo> StatementPasses)
            : this(Conditions,
                   StatementPasses.Select(BodyStatementPass.ToBodyPass).ToArray())
        { }
        public PassPreferences(
			IEnumerable<PassCondition> Conditions,
            IEnumerable<MethodPassInfo> MethodPasses,
            IEnumerable<RootPassInfo> RootPasses)
            : this(Conditions, MethodPasses, RootPasses, 
                   Enumerable.Empty<SignaturePassInfo>())
        { }
		public PassPreferences(
			IEnumerable<PassCondition> Conditions,
			IEnumerable<MethodPassInfo> MethodPasses,
			IEnumerable<RootPassInfo> RootPasses,
			IEnumerable<SignaturePassInfo> SignaturePasses)
			: this(Conditions, MethodPasses, Enumerable.Empty<MethodPassInfo>(), RootPasses, SignaturePasses)
		{ }
		public PassPreferences(
			IEnumerable<PassCondition> Conditions,
			IEnumerable<MethodPassInfo> MethodPasses,
			IEnumerable<MethodPassInfo> LoweringPasses,
			IEnumerable<RootPassInfo> RootPasses,
			IEnumerable<SignaturePassInfo> SignaturePasses)
		{
			this.Conditions = Conditions;
			this.MethodPasses = MethodPasses;
			this.LoweringPasses = LoweringPasses;
			this.RootPasses = RootPasses;
			this.SignaturePasses = SignaturePasses;
		}

        /// <summary>
        /// Gets a sequence of sufficient conditions for
        /// passes to run.
        /// </summary>
        public IEnumerable<PassCondition> Conditions { get; private set; }

        /// <summary>
        /// Gets a sequence of method passes.
        /// </summary>
        public IEnumerable<MethodPassInfo> MethodPasses { get; private set; }

		/// <summary>
		/// Gets a sequence of machine lowering passes.
		/// </summary>
		public IEnumerable<MethodPassInfo> LoweringPasses { get; private set; }

        /// <summary>
        /// Gets a sequence of root passes.
        /// </summary>
        public IEnumerable<RootPassInfo> RootPasses { get; private set; }

        /// <summary>
        /// Gets a sequence of signature passes.
        /// </summary>
        public IEnumerable<SignaturePassInfo> SignaturePasses { get; private set; }

        /// <summary>
        /// Takes the union of these pass
        /// preferences and the given other 
        /// pass preferences.
        /// </summary>
        /// <param name="Other"></param>
        /// <returns></returns>
        public PassPreferences Union(PassPreferences Other)
        {
            return new PassPreferences(
				Conditions.Concat(Other.Conditions),
                MethodPasses.Union(Other.MethodPasses),
				LoweringPasses.Union(Other.LoweringPasses),
				RootPasses.Union(Other.RootPasses),
                SignaturePasses.Union(Other.SignaturePasses));
        }

		/// <summary>
		/// Creates a pass manager that is equivalent to this pass
		/// preference set.
		/// </summary>
		public PassManager ToManager()
		{
			return new PassManager(this);
		}
    }
}
