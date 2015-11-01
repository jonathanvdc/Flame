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
            
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(LowerLambdaPass.Instance, LowerLambdaPassName));
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(TailRecursionPass.Instance, TailRecursionPass.TailRecursionPassName, (optInfo, isPref) => isPref || optInfo.OptimizeMinimal));

            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(InliningPass.Instance, InliningPass.InliningPassName, (optInfo, isPref) => optInfo.OptimizeExperimental));
            RegisterStatementPass(new PassInfo<IStatement, IStatement>(SimplifyFlowPass.Instance, SimplifyFlowPassName, (optInfo, isPref) => (optInfo.OptimizeMinimal && isPref) || optInfo.OptimizeNormal || optInfo.OptimizeSize));
            RegisterStatementPass(new PassInfo<IStatement, IStatement>(Flame.Optimization.Variables.DefinitionPropagationPass.Instance, PropagateLocalsName, (optInfo, isPref) => optInfo.OptimizeExperimental));
            RegisterStatementPass(new PassInfo<IStatement, IStatement>(Flame.Optimization.ImperativeCodePass.Instance, Flame.Optimization.ImperativeCodePass.ImperativeCodePassName));
        }

        public static List<PassInfo<BodyPassArgument, IStatement>> MethodPasses { get; private set; }

        public const string EliminateDeadCodePassName = "dead-code-elimination";
        public const string InitializationPassName = "initialization";
        public const string LowerYieldPassName = "lower-yield";
        public const string LowerLambdaPassName = "lower-lambda";
        public const string SimplifyFlowPassName = "simplify-flow";
        public const string PropagateLocalsName = "propagate-locals";

        public static void RegisterAnalysisPass(PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> Pass)
        {
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(new BodyAnalysisPass(Pass.Pass), Pass.Name, Pass.UsePass));
        }
        public static void RegisterMethodPass(PassInfo<BodyPassArgument, IStatement> Pass)
        {
            MethodPasses.Add(Pass);
        }
        public static void RegisterStatementPass(PassInfo<IStatement, IStatement> Pass)
        {
            RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(new BodyStatementPass(Pass.Pass), Pass.Name, Pass.UsePass));
        }

        private static void AddPass<TIn, TOut>(List<IPass<TIn, TOut>> Passes, PassInfo<TIn, TOut> Info, OptimizationInfo OptInfo, HashSet<string> PreferredPasses)
        {
            if (OptInfo.Log.Options.GetFlag(Info.Name, Info.UsePass(OptInfo, PreferredPasses.Contains(Info.Name))))
            {
                Passes.Add(Info.Pass);
            }
        }

        private static void AddPassName<TIn, TOut>(List<string> Passes, PassInfo<TIn, TOut> Info, OptimizationInfo OptInfo, HashSet<string> PreferredPasses)
        {
            if (OptInfo.Log.Options.GetFlag(Info.Name, Info.UsePass(OptInfo, PreferredPasses.Contains(Info.Name))))
            {
                Passes.Add(Info.Name);
            }
        }

        public static IEnumerable<string> GetSelectedPassNames(ICompilerLog Log, PassPreferences Preferences)
        {
            var names = new List<string>();
            var optInfo = new OptimizationInfo(Log);

            var preferredPassSet = new HashSet<string>(Preferences.PreferredPasses);

            foreach (var item in Preferences.AdditionalPasses.Union(MethodPasses))
            {
                AddPassName(names, item, optInfo, preferredPassSet);
            }

            return names;
        }

        public static PassSuite CreateSuite(ICompilerLog Log, PassPreferences Preferences)
        {
            var preferredPassSet = new HashSet<string>(Preferences.PreferredPasses);

            var optInfo = new OptimizationInfo(Log);

            var methodOpt = Log.GetMethodOptimizer();

            var selectedMethodPasses = new List<IPass<BodyPassArgument, IStatement>>();
            foreach (var item in Preferences.AdditionalPasses.Union(MethodPasses))
            {
                AddPass(selectedMethodPasses, item, optInfo, preferredPassSet);
            }

            return new PassSuite(methodOpt, selectedMethodPasses.Aggregate());
        }
    }
}
