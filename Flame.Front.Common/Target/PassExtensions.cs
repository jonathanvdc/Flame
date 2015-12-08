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
    public static class PassExtensions
    {
        static PassExtensions()
        {
            MethodPasses = new List<PassInfo<BodyPassArgument, IStatement>>();
            PassConditions = new List<PassCondition>();

            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(SlimLambdaPass.Instance, SlimLambdaPass.SlimLambdaPassName));
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(LowerLambdaPass.Instance, LowerLambdaPassName));
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(Flame.Optimization.FlattenInitializationPass.Instance, Flame.Optimization.FlattenInitializationPass.FlattenInitializationPassName));
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(TailRecursionPass.Instance, TailRecursionPass.TailRecursionPassName));
            RegisterPassCondition(TailRecursionPass.TailRecursionPassName, optInfo => optInfo.OptimizeNormal);

            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(InliningPass.Instance, InliningPass.InliningPassName));
            RegisterPassCondition(InliningPass.InliningPassName, optInfo => optInfo.OptimizeExperimental);
            RegisterStatementPass(new PassInfo<IStatement, IStatement>(SimplifyFlowPass.Instance, SimplifyFlowPassName));
            RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeNormal);
            RegisterPassCondition(SimplifyFlowPassName, optInfo => optInfo.OptimizeSize);
            RegisterStatementPass(new PassInfo<IStatement, IStatement>(Flame.Optimization.Variables.DefinitionPropagationPass.Instance, PropagateLocalsName));
            RegisterPassCondition(PropagateLocalsName, optInfo => optInfo.OptimizeExperimental);
            RegisterStatementPass(new PassInfo<IStatement, IStatement>(Flame.Optimization.ImperativeCodePass.Instance, Flame.Optimization.ImperativeCodePass.ImperativeCodePassName));
        }

        public static List<PassInfo<BodyPassArgument, IStatement>> MethodPasses { get; private set; }
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
        public static void RegisterMethodPass(PassInfo<BodyPassArgument, IStatement> Pass)
        {
            MethodPasses.Add(Pass);
        }

        /// <summary>
        /// Registers the given statement pass.
        /// </summary>
        /// <param name="Pass"></param>
        public static void RegisterStatementPass(PassInfo<IStatement, IStatement> Pass)
        {
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(new BodyStatementPass(Pass.Pass), Pass.Name));
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
        /// Gets all passes that are selected by the
        /// given compiler log and pass preferences.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Preferences"></param>
        /// <returns></returns>
        public static IEnumerable<PassInfo<BodyPassArgument, IStatement>> GetSelectedPasses(ICompilerLog Log, PassPreferences Preferences)
        {
            var optInfo = new OptimizationInfo(Log);

            var conditionDict = CreatePassConditionDictionary(PassConditions.Concat(Preferences.AdditionalConditions));

            var selectedPasses = new List<PassInfo<BodyPassArgument, IStatement>>();
            foreach (var item in Preferences.AdditionalPasses.Union(MethodPasses))
            {
                AddPassInfo(selectedPasses, item, optInfo, conditionDict);
            }

            return selectedPasses;
        }

        /// <summary>
        /// Gets the names of all selected passes.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Preferences"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetSelectedPassNames(ICompilerLog Log, PassPreferences Preferences)
        {
            return GetSelectedPasses(Log, Preferences).Select(item => item.Name);
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
            var methodOpt = Log.GetMethodOptimizer();

            var selectedMethodPasses = GetSelectedPasses(Log, Preferences).Select(item => item.Pass);

            return new PassSuite(methodOpt, selectedMethodPasses.Aggregate());
        }
    }
}
