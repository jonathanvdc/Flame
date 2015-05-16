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
    public struct PassInfo<TIn, TOut>
    {
        public PassInfo(IPass<TIn, TOut> Pass, string Name, bool IsDefault)
        {
            this = default(PassInfo<TIn, TOut>);
            this.Pass = Pass;
            this.Name = Name;
            this.IsDefault = IsDefault;
        }

        public IPass<TIn, TOut> Pass { get; private set; }
        public string Name { get; private set; }
        public bool IsDefault { get; private set; }        
    }

    public static class PassExtensions
    {
        static PassExtensions()
        {
            MethodPasses = new List<PassInfo<BodyPassArgument, IStatement>>();
            StatementPasses = new List<PassInfo<IStatement, IStatement>>();

            MethodPasses.Add(new PassInfo<BodyPassArgument, IStatement>(LowerYieldPass.Instance, LowerYieldPassName, false));
        }

        public static List<PassInfo<BodyPassArgument, IStatement>> MethodPasses { get; private set; }
        public static List<PassInfo<IStatement, IStatement>> StatementPasses { get; private set; }

        public static string LowerYieldPassName = "lower-yield";

        private static void AddPass<TIn, TOut>(List<IPass<TIn, TOut>> Passes, PassInfo<TIn, TOut> Info, ICompilerLog Log, HashSet<string> PreferredPasses)
        {
            if (Log.Options.GetOption<bool>("f" + Info.Name, Info.IsDefault || PreferredPasses.Contains(Info.Name)))
            {
                Passes.Add(Info.Pass);
            }
        }

        public static PassSuite CreateSuite(ICompilerLog Log, IEnumerable<string> PreferredPasses)
        {
            var preferredPassSet = new HashSet<string>(PreferredPasses);

            var methodOpt = Log.GetMethodOptimizer();

            var selectedMethodPasses = new List<IPass<BodyPassArgument, IStatement>>();
            foreach (var item in MethodPasses)
            {
                AddPass(selectedMethodPasses, item, Log, preferredPassSet);
            }

            var selectedStatementPassSet = new List<IPass<IStatement, IStatement>>();
            foreach (var item in StatementPasses)
            {
                AddPass(selectedStatementPassSet, item, Log, preferredPassSet);
            }

            return new PassSuite(methodOpt, selectedMethodPasses.Aggregate(), selectedStatementPassSet.Aggregate());
        }
    }
}
