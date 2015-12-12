using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Optimization;
using Flame.Recompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    using MethodPassInfo = PassInfo<BodyPassArgument, IStatement>;
    using SignaturePassInfo = PassInfo<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using StatementPassInfo = PassInfo<IStatement, IStatement>;
    using RootPassInfo = PassInfo<BodyPassArgument, IEnumerable<IMember>>;
    using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;
    using ISignaturePass = IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using Flame.Front.Passes;

    public static class PassExtensions
    {
        static PassExtensions()
        {
            MethodPasses = new List<MethodPassInfo>();
            RootPasses = new List<RootPassInfo>();
            SignaturePasses = new List<SignaturePassInfo>();
            PassConditions = new List<PassCondition>();

            RegisterMethodPass(new MethodPassInfo(SlimLambdaPass.Instance, SlimLambdaPass.SlimLambdaPassName));
            RegisterMethodPass(new MethodPassInfo(LowerLambdaPass.Instance, LowerLambdaPassName));
            RegisterMethodPass(new MethodPassInfo(Flame.Optimization.FlattenInitializationPass.Instance, Flame.Optimization.FlattenInitializationPass.FlattenInitializationPassName));
            RegisterMethodPass(new MethodPassInfo(TailRecursionPass.Instance, TailRecursionPass.TailRecursionPassName));
            RegisterPassCondition(TailRecursionPass.TailRecursionPassName, optInfo => optInfo.OptimizeNormal);

            RegisterMethodPass(new MethodPassInfo(InliningPass.Instance, InliningPass.InliningPassName));
            RegisterPassCondition(InliningPass.InliningPassName, optInfo => optInfo.OptimizeAggressive);
            RegisterStatementPass(new StatementPassInfo(SimplifyFlowPass.Instance, SimplifyFlowPassName));
            RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeNormal);
            RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeSize);
            RegisterStatementPass(new StatementPassInfo(Flame.Optimization.Variables.DefinitionPropagationPass.Instance, PropagateLocalsName));
            RegisterPassCondition(PropagateLocalsName, optInfo => optInfo.OptimizeAggressive);
            RegisterStatementPass(new StatementPassInfo(Flame.Optimization.ImperativeCodePass.Instance, Flame.Optimization.ImperativeCodePass.ImperativeCodePassName));

            RegisterRootPass(new RootPassInfo(GenerateStaticPass.Instance, GenerateStaticPass.GenerateStaticPassName));

            // Register -fnormalize-names-clr here, because the IR back-end could also use
            // this pass when targeting the CLR platform indirectly.
            RegisterSignaturePass(new SignaturePassInfo(Flame.Cecil.NormalizeNamesPass.Instance, Flame.Cecil.NormalizeNamesPass.NormalizeNamesPassName));

            // -fwrap-extension-properties is actually a set of two passes which are
            // always on or off at the same time.
            RegisterRootPass(new RootPassInfo(WrapExtensionPropertiesPass.RootPassInstance, WrapExtensionPropertiesPass.WrapExtensionPropertiesPassName));
            RegisterSignaturePass(new SignaturePassInfo(WrapExtensionPropertiesPass.SignaturePassInstance, WrapExtensionPropertiesPass.WrapExtensionPropertiesPassName));
        }

        /// <summary>
        /// Gets the list of all globally available method passes.
        /// </summary>
        public static List<MethodPassInfo> MethodPasses { get; private set; }

        /// <summary>
        /// Gets the list of all globally available root passes.
        /// </summary>
        public static List<RootPassInfo> RootPasses { get; private set; }

        /// <summary>
        /// Gets the list of all globally available signature passes.
        /// </summary>
        public static List<SignaturePassInfo> SignaturePasses { get; private set; }

        /// <summary>
        /// Gets the list of all globally available pass conditions. 
        /// </summary>
        public static List<PassCondition> PassConditions { get; private set; }

        public const string EliminateDeadCodePassName = "dead-code-elimination";
        public const string InitializationPassName = "initialization";
        public const string LowerYieldPassName = "lower-yield";
        public const string LowerLambdaPassName = "lower-lambda";
        public const string SimplifyFlowPassName = "simplify-flow";
        public const string PropagateLocalsName = "propagate-locals";

        /// <summary>
        /// Registers the given analysis pass.
        /// </summary>
        /// <param name="Pass"></param>
        public static void RegisterAnalysisPass(PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> Pass)
        {
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(new BodyAnalysisPass(Pass.Pass), Pass.Name));
        }

        /// <summary>
        /// Registers the given method body pass.
        /// </summary>
        /// <param name="Pass"></param>
        public static void RegisterMethodPass(MethodPassInfo Pass)
        {
            MethodPasses.Add(Pass);
        }

        /// <summary>
        /// Registers the given statement pass.
        /// </summary>
        /// <param name="Pass"></param>
        public static void RegisterStatementPass(StatementPassInfo Pass)
        {
            RegisterMethodPass(new MethodPassInfo(new BodyStatementPass(Pass.Pass), Pass.Name));
        }

        /// <summary>
        /// Registers the given root pass.
        /// </summary>
        /// <param name="Pass"></param>
        public static void RegisterRootPass(RootPassInfo Pass)
        {
            RootPasses.Add(Pass);
        }

        /// <summary>
        /// Registers the given signature pass.
        /// </summary>
        /// <param name="Pass"></param>
        public static void RegisterSignaturePass(SignaturePassInfo Pass)
        {
            SignaturePasses.Add(Pass);
        }

        /// <summary>
        /// Registers a sufficient condition for a 
        /// pass to be run.
        /// </summary>
        /// <param name="Condition"></param>
        public static void RegisterPassCondition(PassCondition Condition)
        {
            PassConditions.Add(Condition);
        }

        /// <summary>
        /// Registers a sufficient condition for the 
        /// pass with the given name to be run.
        /// </summary>
        /// <param name="PassName"></param>
        /// <param name="Condition"></param>
        public static void RegisterPassCondition(string PassName, Func<OptimizationInfo, bool> Condition)
        {
            RegisterPassCondition(new PassCondition(PassName, Condition));
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
        private static IRootPass Aggregate(IEnumerable<IRootPass> RootPasses)
        {
            return RootPasses.Aggregate<IRootPass, IRootPass>(EmptyRootPass.Instance, (result, item) => new AggregateRootPass(result, item));
        }

        /// <summary>
        /// Creates an aggregate signature pass from the given sequence
        /// of signature passes.
        /// </summary>
        /// <param name="RootPasses"></param>
        /// <returns></returns>
        private static ISignaturePass Aggregate(IEnumerable<ISignaturePass> RootPasses)
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
        public static Tuple<IEnumerable<MethodPassInfo>, IEnumerable<RootPassInfo>, IEnumerable<SignaturePassInfo>> GetSelectedPasses(
            OptimizationInfo OptInfo, PassPreferences Preferences)
        {
            var conditionDict = CreatePassConditionDictionary(PassConditions.Concat(Preferences.AdditionalConditions));

            var selectedMethodPasses = new List<MethodPassInfo>();
            foreach (var item in Preferences.AdditionalMethodPasses.Union(MethodPasses))
            {
                AddPassInfo(selectedMethodPasses, item, OptInfo, conditionDict);
            }

            var selectedRootPasses = new List<RootPassInfo>();
            foreach (var item in Preferences.AdditionalRootPasses.Union(RootPasses))
            {
                AddPassInfo(selectedRootPasses, item, OptInfo, conditionDict);
            }

            var selectedSignaturePasses = new List<SignaturePassInfo>();
            foreach (var item in Preferences.AdditionalSignaturePasses.Union(SignaturePasses))
            {
                AddPassInfo(selectedSignaturePasses, item, OptInfo, conditionDict);
            }

            return Tuple.Create<IEnumerable<MethodPassInfo>, IEnumerable<RootPassInfo>, IEnumerable<SignaturePassInfo>>(
                selectedMethodPasses, selectedRootPasses, selectedSignaturePasses);
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
        public static IReadOnlyDictionary<string, IEnumerable<string>> GetSelectedPassNames(OptimizationInfo OptInfo, PassPreferences Preferences)
        {
            var selected = GetSelectedPasses(OptInfo, Preferences);

            return new Dictionary<string, IEnumerable<string>>()
            {
                { "Signature", selected.Item3.Select(item => item.Name) },
                { "Body", selected.Item1.Select(item => item.Name) },
                { "Root", selected.Item2.Select(item => item.Name) }
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
        public static IReadOnlyDictionary<string, IEnumerable<string>> GetSelectedPassNames(ICompilerLog Log, PassPreferences Preferences)
        {
            return GetSelectedPassNames(new OptimizationInfo(Log), Preferences);
        }

        /// <summary>
        /// Creates a pass suite from the given compiler log and
        /// pass preferences.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Preferences"></param>
        /// <returns></returns>
        public static PassSuite CreateSuite(ICompilerLog Log, PassPreferences Preferences)
        {
            var optInfo = new OptimizationInfo(Log);

            // Call the `Optimize` method on method body statements
            // if `-O1` or above is given. (`-Og` == `-O1 -g` is the 
            // default optimization level. `-O0` should only be used
            // when hacking the compiler or something)
            var methodOpt = new DefaultOptimizer(optInfo.OptimizeMinimal);

            // Select passes by relying on the optimization info
            // and pass preferences.
            var selectedPasses = GetSelectedPasses(optInfo, Preferences);
            var selectedMethodPasses = selectedPasses.Item1.Select(item => item.Pass);
            var selectedRootPasses = selectedPasses.Item2.Select(item => item.Pass);
            var selectedSigPasses = selectedPasses.Item3.Select(item => item.Pass);

            return new PassSuite(methodOpt, selectedMethodPasses.Aggregate(), Aggregate(selectedRootPasses), Aggregate(selectedSigPasses));
        }
    }
}
