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
    public struct PassInfo<TIn, TOut> : IEquatable<PassInfo<TIn, TOut>>
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

    public class PassPreferences
    {
        public PassPreferences(params string[] PreferredPasses)
            : this((IEnumerable<string>)PreferredPasses)
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses)
            : this(PreferredPasses,
                   Enumerable.Empty<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>>())
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses,
                               IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalPrePasses)
            : this(PreferredPasses, AdditionalPrePasses,
                   Enumerable.Empty<PassInfo<BodyPassArgument, IStatement>>())
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses,
                               IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalPrePasses,
                               IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalMethodPasses)
            : this(PreferredPasses, AdditionalPrePasses, AdditionalMethodPasses,
                   Enumerable.Empty<PassInfo<IStatement, IStatement>>())
        {
        }
        public PassPreferences(IEnumerable<string> PreferredPasses,
                               IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalPrePasses,
                               IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalMethodPasses,
                               IEnumerable<PassInfo<IStatement, IStatement>> AdditionalPasses)
        {
            this.PreferredPasses = PreferredPasses;
            this.AdditionalPrePasses = AdditionalPrePasses;
            this.AdditionalMethodPasses = AdditionalMethodPasses;
            this.AdditionalPasses = AdditionalPasses;
        }

        public IEnumerable<string> PreferredPasses { get; private set; }
        public IEnumerable<PassInfo<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>> AdditionalPrePasses { get; private set; }
        public IEnumerable<PassInfo<BodyPassArgument, IStatement>> AdditionalMethodPasses { get; private set; }
        public IEnumerable<PassInfo<IStatement, IStatement>> AdditionalPasses { get; private set; }

        public PassPreferences Union(PassPreferences Other)
        {
            return new PassPreferences(PreferredPasses.Union(Other.PreferredPasses),
                AdditionalPrePasses.Union(Other.AdditionalPrePasses),
                AdditionalMethodPasses.Union(Other.AdditionalMethodPasses),
                AdditionalPasses.Union(Other.AdditionalPasses));
        }
    }

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
