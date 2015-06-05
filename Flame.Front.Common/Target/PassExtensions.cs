using Flame.Compiler;
using Flame.Compiler.Visitors;
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

            MethodPasses.Add(new PassInfo<BodyPassArgument, IStatement>(LowerYieldPass.Instance, LowerYieldPassName, false));
        }

        public static List<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> PreStatementPasses { get; private set; }
        public static List<PassInfo<BodyPassArgument, IStatement>> MethodPasses { get; private set; }
        public static List<PassInfo<IStatement, IStatement>> StatementPasses { get; private set; }

        public const string EliminateDeadCodePassName = "dead-code-elimination";
        public const string LowerYieldPassName = "lower-yield";

        private static void AddPass<TIn, TOut>(List<IPass<TIn, TOut>> Passes, PassInfo<TIn, TOut> Info, ICompilerLog Log, HashSet<string> PreferredPasses)
        {
            if (Log.Options.GetOption<bool>("f" + Info.Name, Info.IsDefault || PreferredPasses.Contains(Info.Name)))
            {
                Passes.Add(Info.Pass);
            }
        }

        public static PassSuite CreateSuite(ICompilerLog Log, PassPreferences Preferences)
        {
            var preferredPassSet = new HashSet<string>(Preferences.PreferredPasses);

            var methodOpt = Log.GetMethodOptimizer();

            var selectedPreStatementPasses = new List<IPass<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>>();
            foreach (var item in Preferences.AdditionalPrePasses.Union(PreStatementPasses))
            {
                AddPass(selectedPreStatementPasses, item, Log, preferredPassSet);
            }

            var selectedMethodPasses = new List<IPass<BodyPassArgument, IStatement>>();
            foreach (var item in Preferences.AdditionalMethodPasses.Union(MethodPasses))
            {
                AddPass(selectedMethodPasses, item, Log, preferredPassSet);
            }

            var selectedStatementPassSet = new List<IPass<IStatement, IStatement>>();
            foreach (var item in Preferences.AdditionalPasses.Union(StatementPasses))
            {
                AddPass(selectedStatementPassSet, item, Log, preferredPassSet);
            }

            return new PassSuite(methodOpt, selectedPreStatementPasses.Aggregate(), selectedMethodPasses.Aggregate(), selectedStatementPassSet.Aggregate());
        }
    }
}
