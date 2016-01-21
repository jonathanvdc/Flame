using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Visitors;
using Flame.Compiler;
using Flame.Recompilation;
using Flame.Front.Target;

namespace Flame.Front.Passes
{    
	using AnalysisPassInfo = PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>;
	using MethodPassInfo = PassInfo<BodyPassArgument, IStatement>;
	using SignaturePassInfo = PassInfo<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
	using StatementPassInfo = PassInfo<IStatement, IStatement>;
	using RootPassInfo = PassInfo<BodyPassArgument, IEnumerable<IMember>>;
	using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;
	using ISignaturePass = IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;

	/// <summary>
	/// A class of objects that manage a sequence of passes and their associated conditions.
	/// </summary>
	public class PassManager
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Flame.Front.Passes.PassManager"/> class.
		/// </summary>
		public PassManager()
		{
			this.MethodPasses = new List<MethodPassInfo>();
			this.LoweringPasses = new List<MethodPassInfo>();
			this.RootPasses = new List<RootPassInfo>();
			this.SignaturePasses = new List<SignaturePassInfo>();
			this.PassConditions = new List<PassCondition>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Flame.Front.Passes.PassManager"/> class.
		/// </summary>
		/// <param name="Preferences">A set of pass preferences.</param>
		public PassManager(PassPreferences Preferences)
		{
			this.MethodPasses = new List<MethodPassInfo>(Preferences.MethodPasses);
			this.LoweringPasses = new List<MethodPassInfo>(Preferences.LoweringPasses);
			this.RootPasses = new List<RootPassInfo>(Preferences.RootPasses);
			this.SignaturePasses = new List<SignaturePassInfo>(Preferences.SignaturePasses);
			this.PassConditions = new List<PassCondition>(Preferences.Conditions);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Flame.Front.Passes.PassManager"/> class.
		/// </summary>
		/// <param name="Other">The pass manager to copy.</param>
		public PassManager(PassManager Other)
		{ 
			this.MethodPasses = new List<MethodPassInfo>(Other.MethodPasses);
			this.LoweringPasses = new List<MethodPassInfo>(Other.LoweringPasses);
			this.RootPasses = new List<RootPassInfo>(Other.RootPasses);
			this.SignaturePasses = new List<SignaturePassInfo>(Other.SignaturePasses);
			this.PassConditions = new List<PassCondition>(Other.PassConditions);
		}

		/// <summary>
		/// Gets the list of method passes, which analyze and/or
		/// optimize method bodies.
		/// </summary>
		public List<MethodPassInfo> MethodPasses { get; private set; }

		/// <summary>
		/// Gets a list of machine lowering passes, which perform
		/// lowering and target-specific optimizations.
		/// </summary>
		public List<MethodPassInfo> LoweringPasses { get; private set; }

		/// <summary>
		/// Gets a list of root passes.
		/// </summary>
		public List<RootPassInfo> RootPasses { get; private set; }

		/// <summary>
		/// Gets a list of signature passes.
		/// </summary>
		public List<SignaturePassInfo> SignaturePasses { get; private set; }

		/// <summary>
		/// Gets a list of pass conditions.
		/// </summary>
		public List<PassCondition> PassConditions { get; private set; }

		/// <summary>
		/// Registers the given analysis pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterMethodPass(PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> Pass)
		{
			RegisterMethodPass(new MethodPassInfo(new BodyAnalysisPass(Pass.Pass), Pass.Name));
		}

		/// <summary>
		/// Registers the given method body pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterMethodPass(MethodPassInfo Pass)
		{
			MethodPasses.Add(Pass);
		}

		/// <summary>
		/// Registers the given statement pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterMethodPass(StatementPassInfo Pass)
		{
			RegisterMethodPass(new MethodPassInfo(new BodyStatementPass(Pass.Pass), Pass.Name));
		}

		/// <summary>
		/// Registers the given analysis pass as a machine lowering pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterLoweringPass(PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> Pass)
		{
			RegisterLoweringPass(new MethodPassInfo(new BodyAnalysisPass(Pass.Pass), Pass.Name));
		}

		/// <summary>
		/// Registers the given method body pass as a machine lowering pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterLoweringPass(MethodPassInfo Pass)
		{
			LoweringPasses.Add(Pass);
		}

		/// <summary>
		/// Registers the given statement pass as a machine lowering pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterLoweringPass(StatementPassInfo Pass)
		{
			RegisterLoweringPass(new MethodPassInfo(new BodyStatementPass(Pass.Pass), Pass.Name));
		}

		/// <summary>
		/// Registers the given root pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterRootPass(RootPassInfo Pass)
		{
			RootPasses.Add(Pass);
		}

		/// <summary>
		/// Registers the given signature pass.
		/// </summary>
		/// <param name="Pass"></param>
		public void RegisterSignaturePass(SignaturePassInfo Pass)
		{
			SignaturePasses.Add(Pass);
		}

		/// <summary>
		/// Registers a sufficient condition for a
		/// pass to be run.
		/// </summary>
		/// <param name="Condition"></param>
		public void RegisterPassCondition(PassCondition Condition)
		{
			PassConditions.Add(Condition);
		}

		/// <summary>
		/// Registers a sufficient condition for the
		/// pass with the given name to be run.
		/// </summary>
		/// <param name="PassName"></param>
		/// <param name="Condition"></param>
		public void RegisterPassCondition(string PassName, Func<OptimizationInfo, bool> Condition)
		{
			RegisterPassCondition(new PassCondition(PassName, Condition));
		}

		/// <summary>
		/// Prepend the specified pass preferences to this pass manager.
		/// </summary>
		public void Prepend(PassPreferences Preferences)
		{
			this.MethodPasses.InsertRange(0, Preferences.MethodPasses);
			this.LoweringPasses.InsertRange(0, Preferences.LoweringPasses);
			this.RootPasses.InsertRange(0, Preferences.RootPasses);
			this.SignaturePasses.InsertRange(0, Preferences.SignaturePasses);
			this.PassConditions.InsertRange(0, Preferences.Conditions);
		}

		/// <summary>
		/// Append the specified pass preferences to this pass manager.
		/// </summary>
		public void Append(PassPreferences Preferences)
		{
			this.MethodPasses.AddRange(Preferences.MethodPasses);
			this.LoweringPasses.AddRange(Preferences.LoweringPasses);
			this.RootPasses.AddRange(Preferences.RootPasses);
			this.SignaturePasses.AddRange(Preferences.SignaturePasses);
			this.PassConditions.AddRange(Preferences.Conditions);
		}

		/// <summary>
		/// Converts this pass manager to a set of pass preferences.
		/// </summary>
		public PassPreferences ToPreferences()
		{
			return new PassPreferences(
				PassConditions, MethodPasses, LoweringPasses, 
				RootPasses, SignaturePasses);
		}

		/// <summary>
		/// Checks if any of the conditions for
		/// the pass with the given name are satisfied
		/// by the given optimization info.
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="OptInfo"></param>
		/// <param name="PassConditions"></param>
		/// <returns></returns>
		private static bool AnyConditionSatisfied(
			string Name, OptimizationInfo OptInfo,
			IReadOnlyDictionary<string, IEnumerable<Func<OptimizationInfo, bool>>> PassConditions)
		{
			IEnumerable<Func<OptimizationInfo, bool>> conds;
			if (!PassConditions.TryGetValue(Name, out conds))
			{
				return false;
			}
			return conds.Any(item => item(OptInfo));
		}

		private static void AddPassInfo<TIn, TOut>(
			List<PassInfo<TIn, TOut>> Passes, PassInfo<TIn, TOut> Info,
			OptimizationInfo OptInfo,
			IReadOnlyDictionary<string, IEnumerable<Func<OptimizationInfo, bool>>> PassConditions)
		{
			if (OptInfo.Log.Options.GetFlag(Info.Name, AnyConditionSatisfied(Info.Name, OptInfo, PassConditions)))
			{
				Passes.Add(Info);
			}
		}

		/// <summary>
		/// Creates a dictionary that maps pass names to a sequence
		/// of sufficient conditions from the given sequence of
		/// pass conditions.
		/// </summary>
		/// <param name="Conditions"></param>
		/// <returns></returns>
		private static Dictionary<string, IEnumerable<Func<OptimizationInfo, bool>>> CreatePassConditionDictionary(
			IEnumerable<PassCondition> Conditions)
		{
			var results = new Dictionary<string, IEnumerable<Func<OptimizationInfo, bool>>>();
			foreach (var item in Conditions)
			{
				IEnumerable<Func<OptimizationInfo, bool>> itemSet;
				if (!results.TryGetValue(item.PassName, out itemSet))
				{
					itemSet = new List<Func<OptimizationInfo, bool>>();
					results[item.PassName] = itemSet;
				}
				((List<Func<OptimizationInfo, bool>>)itemSet).Add(item.Condition);
			}
			return results;
		}

		/// <summary>
		/// Creates an aggregate root pass from the given sequence
		/// of root passes.
		/// </summary>
		/// <param name="RootPasses"></param>
		/// <returns></returns>
		private IRootPass Aggregate(IEnumerable<IRootPass> RootPasses)
		{
			return RootPasses.Aggregate<IRootPass, IRootPass>(EmptyRootPass.Instance, (result, item) => new AggregateRootPass(result, item));
		}

		/// <summary>
		/// Creates an aggregate signature pass from the given sequence
		/// of signature passes.
		/// </summary>
		/// <param name="RootPasses"></param>
		/// <returns></returns>
		private ISignaturePass Aggregate(IEnumerable<ISignaturePass> RootPasses)
		{
			return RootPasses.Aggregate<ISignaturePass, ISignaturePass>(EmptyMemberSignaturePass<IMember>.Instance,
				(result, item) => new AggregateMemberSignaturePass<IMember>(result, item));
		}

		/// <summary>
		/// Gets all passes that are selected by the
		/// given optimization info and pass preferences.
		/// </summary>
		/// <param name="Log"></param>
		/// <param name="Preferences"></param>
		/// <returns></returns>
		public Tuple<IEnumerable<MethodPassInfo>, IEnumerable<MethodPassInfo>, IEnumerable<RootPassInfo>, IEnumerable<SignaturePassInfo>> GetSelectedPasses(
			OptimizationInfo OptInfo)
		{
			var conditionDict = CreatePassConditionDictionary(PassConditions);

			var selectedMethodPasses = new List<MethodPassInfo>();
			foreach (var item in MethodPasses)
			{
				AddPassInfo(selectedMethodPasses, item, OptInfo, conditionDict);
			}

			var selectedLoweringPasses = new List<MethodPassInfo>();
			foreach (var item in LoweringPasses)
			{
				AddPassInfo(selectedLoweringPasses, item, OptInfo, conditionDict);
			}

			var selectedRootPasses = new List<RootPassInfo>();
			foreach (var item in RootPasses)
			{
				AddPassInfo(selectedRootPasses, item, OptInfo, conditionDict);
			}

			var selectedSignaturePasses = new List<SignaturePassInfo>();
			foreach (var item in SignaturePasses)
			{
				AddPassInfo(selectedSignaturePasses, item, OptInfo, conditionDict);
			}

			return Tuple.Create<IEnumerable<MethodPassInfo>, IEnumerable<MethodPassInfo>, IEnumerable<RootPassInfo>, IEnumerable<SignaturePassInfo>>(
				selectedMethodPasses, selectedLoweringPasses, selectedRootPasses, selectedSignaturePasses);
		}

		/// <summary>
		/// Gets the names of all passes that are selected by the
		/// given optimization info and pass preferences. The
		/// results are returned as a dictionary that maps pass types
		/// to a sequence of selected pass names.
		/// </summary>
		/// <param name="Log"></param>
		/// <param name="Preferences"></param>
		/// <returns></returns>
		public IReadOnlyDictionary<string, IEnumerable<string>> GetSelectedPassNames(OptimizationInfo OptInfo)
		{
			var selected = GetSelectedPasses(OptInfo);

			return new Dictionary<string, IEnumerable<string>>()
			{
				{ "Signature", selected.Item4.Select(item => item.Name) },
				{ "Body", selected.Item1.Select(item => item.Name) },
				{ "Lowering", selected.Item2.Select(item => item.Name) },
				{ "Root", selected.Item3.Select(item => item.Name) }
			};
		}


		/// <summary>
		/// Gets the names of all passes that are selected by the
		/// given compiler log and pass preferences. The
		/// results are returned as a dictionary that maps pass types
		/// to a sequence of selected pass names.
		/// </summary>
		/// <param name="Log"></param>
		/// <param name="Preferences"></param>
		/// <returns></returns>
		public IReadOnlyDictionary<string, IEnumerable<string>> GetSelectedPassNames(ICompilerLog Log)
		{
			return GetSelectedPassNames(new OptimizationInfo(Log));
		}

		/// <summary>
		/// Creates a pass suite from the given compiler log and
		/// pass preferences.
		/// </summary>
		/// <param name="Log"></param>
		/// <param name="Preferences"></param>
		/// <returns></returns>
		public PassSuite CreateSuite(ICompilerLog Log)
		{
			var optInfo = new OptimizationInfo(Log);

			// Call the `Optimize` method on method body statements
			// if `-O1` or above is given. (`-Og` == `-O1 -g` is the
			// default optimization level. `-O0` should only be used
			// when hacking the compiler or something)
			var methodOpt = new DefaultOptimizer(optInfo.OptimizeMinimal);

			// Select passes by relying on the optimization info
			// and pass preferences.
			var selectedPasses = GetSelectedPasses(optInfo);
			var selectedMethodPasses = selectedPasses.Item1.Select(item => item.Pass);
			var selectedLoweringPasses = selectedPasses.Item2.Select(item => item.Pass);
			var selectedRootPasses = selectedPasses.Item3.Select(item => item.Pass);
			var selectedSigPasses = selectedPasses.Item4.Select(item => item.Pass);

			return new PassSuite(
				methodOpt, selectedMethodPasses.Aggregate(), 
				selectedLoweringPasses.Aggregate(), 
				Aggregate(selectedRootPasses), 
				Aggregate(selectedSigPasses));
		}
	}
}

