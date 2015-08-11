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
            StatementPasses = new List<PassInfo<IStatement, IStatement>>();
            PreStatementPasses = new List<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>>();

            MethodPasses.Add(new PassInfo<BodyPassArgument, IStatement>(LowerYieldPass.Instance, LowerYieldPassName));
            MethodPasses.Add(new PassInfo<BodyPassArgument, IStatement>(LowerLambdaPass.Instance, LowerLambdaPassName));

            MethodPasses.Add(new PassInfo<BodyPassArgument, IStatement>(InliningPass.Instance, InliningPassName, (optInfo, isPref) => optInfo.OptimizeExperimental));
            StatementPasses.Add(new PassInfo<IStatement, IStatement>(SimplifyFlowPass.Instance, SimplifyFlowPassName, (optInfo, isPref) => (optInfo.OptimizeMinimal && isPref) || optInfo.OptimizeNormal || optInfo.OptimizeSize));
            StatementPasses.Add(new PassInfo<IStatement, IStatement>(Flame.Optimization.Variables.DefinitionPropagationPass.Instance, PropagateLocalsName, (optInfo, isPref) => optInfo.OptimizeExperimental));
        }

        public static List<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> PreStatementPasses { get; private set; }
        public static List<PassInfo<BodyPassArgument, IStatement>> MethodPasses { get; private set; }
        public static List<PassInfo<IStatement, IStatement>> StatementPasses { get; private set; }

        public const string EliminateDeadCodePassName = "dead-code-elimination";
        public const string LowerYieldPassName = "lower-yield";
        public const string LowerLambdaPassName = "lower-lambda";
        public const string InliningPassName = "inline";
        public const string SimplifyFlowPassName = "simplify-flow";
        public const string PropagateLocalsName = "propagate-locals";

        private static void AddPass<TIn, TOut>(List<IPass<TIn, TOut>> Passes, PassInfo<TIn, TOut> Info, OptimizationInfo OptInfo, HashSet<string> PreferredPasses)
        {
            if (OptInfo.Log.Options.GetOption<bool>("f" + Info.Name, Info.UsePass(OptInfo, PreferredPasses.Contains(Info.Name))))
            {
                Passes.Add(Info.Pass);
            }
        }

        private static void AddPassName<TIn, TOut>(List<string> Passes, PassInfo<TIn, TOut> Info, OptimizationInfo OptInfo, HashSet<string> PreferredPasses)
        {
            if (OptInfo.Log.Options.GetOption<bool>("f" + Info.Name, Info.UsePass(OptInfo, PreferredPasses.Contains(Info.Name))))
            {
                Passes.Add(Info.Name);
            }
        }

        public static IEnumerable<string> GetSelectedPassNames(ICompilerLog Log, PassPreferences Preferences)
        {
            var names = new List<string>();
            var optInfo = new OptimizationInfo(Log);

            var preferredPassSet = new HashSet<string>(Preferences.PreferredPasses);

            foreach (var item in Preferences.AdditionalPrePasses.Union(PreStatementPasses))
            {
                AddPassName(names, item, optInfo, preferredPassSet);
            }

            foreach (var item in Preferences.AdditionalMethodPasses.Union(MethodPasses))
            {
                AddPassName(names, item, optInfo, preferredPassSet);
            }

            foreach (var item in Preferences.AdditionalPasses.Union(StatementPasses))
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

            var selectedPreStatementPasses = new List<IPass<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>>();
            foreach (var item in Preferences.AdditionalPrePasses.Union(PreStatementPasses))
            {
                AddPass(selectedPreStatementPasses, item, optInfo, preferredPassSet);
            }

            var selectedMethodPasses = new List<IPass<BodyPassArgument, IStatement>>();
            foreach (var item in Preferences.AdditionalMethodPasses.Union(MethodPasses))
            {
                AddPass(selectedMethodPasses, item, optInfo, preferredPassSet);
            }

            var selectedStatementPassSet = new List<IPass<IStatement, IStatement>>();
            foreach (var item in Preferences.AdditionalPasses.Union(StatementPasses))
            {
                AddPass(selectedStatementPassSet, item, optInfo, preferredPassSet);
            }

            return new PassSuite(methodOpt, selectedPreStatementPasses.Aggregate(), selectedMethodPasses.Aggregate(), selectedStatementPassSet.Aggregate());
        }
    }
}
