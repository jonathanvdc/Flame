using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Optimization;
using Flame.Optimization.Variables;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Flame.Front.Passes;

namespace Flame.Front.Target
{
    /// <summary>
    /// A pass that performs quick-and-dirty inlining, without any recursion.
    /// </summary>
    public sealed class FastInliningPass : InliningPassBase
    {
        private FastInliningPass() { }

        public static readonly FastInliningPass Instance = new FastInliningPass();

        public override Func<IStatement, IStatement> GetBodyOptimizer(BodyPassArgument Argument)
        {
            // Don't optimize statements. Just reconstruct the CFG.
            return ConstructFlowGraphPass.Instance.Apply;
        }

        public override Func<DissectedCall, bool> GetInliningCriteria(BodyPassArgument Argument)
        {
            // Use the same inlining criteria as the normal inlining pass.
            return InliningPass.Instance.GetInliningCriteria(Argument);
        }

        public override int GetMaxRecursion(BodyPassArgument Argument)
        {
            return 1;
        }
    }

    public class ScalarReplacementPass : ScalarReplacementPassBase
    {
        private ScalarReplacementPass()
        { }

        public static readonly ScalarReplacementPass Instance = new ScalarReplacementPass();

        public const string ScalarReplacementInlineToleranceOption = "scalarrepl-" + InliningPass.InlineToleranceOption;
        public const int DefaultScalarReplacementInlineTolerance = InliningPass.DefaultInlineTolerance + DefaultScalarReplacementTolerance;

        public const string ScalarReplacementToleranceOption = "scalarrepl-tolerance";
        // By default, don't replace aggregates that are more than eight words
        // (i.e. eight ints or four longs) wide.
        public const int DefaultScalarReplacementTolerance = 8 * InliningPass.WordSize;

        public static readonly WarningDescription UnknownInlinerArgumentWarning = new WarningDescription("unknown-inliner", Warnings.Instance.Build);

        private IStatement GetMethodBody(BodyPassArgument Value, IMethod Method)
        {
            var result = Value.PassEnvironment.GetMethodBody(Method);

            if (result == null)
            {
                return null;
            }

            return CloningVisitor.Instance.Visit(result);
        }

        private static int GetInlineTolerance(ICompilerOptions Options)
        {
            if (Options.HasOption(ScalarReplacementInlineToleranceOption))
                return Options.GetOption<int>(
                    ScalarReplacementInlineToleranceOption,
                    DefaultScalarReplacementInlineTolerance);
            else if (Options.HasOption(InliningPass.InlineToleranceOption))
                return DefaultScalarReplacementTolerance + Options.GetOption<int>(
                    InliningPass.InlineToleranceOption,
                    InliningPass.DefaultInlineTolerance);
            else
                return DefaultScalarReplacementInlineTolerance;
        }

        public override Func<IType, bool> GetReplacementCriteria(BodyPassArgument Argument)
        {
            var log = Argument.PassEnvironment.Log;

            int tolerance = log.Options.GetOption<int>(
                ScalarReplacementToleranceOption, DefaultScalarReplacementTolerance);
            return ty => InliningPass.ApproximateSize(ty) <= tolerance;
        }

        public override Func<DissectedCall, bool> GetInliningCriteria(BodyPassArgument Argument)
        {
            var log = Argument.PassEnvironment.Log;

            int inlineTolerance = GetInlineTolerance(log.Options);
            return call => InliningPass.ShouldInline(Argument, call, inlineTolerance, false);
        }

        public override int GetMaxRecursion(BodyPassArgument Argument)
        {
            var log = Argument.PassEnvironment.Log;
            return log.Options.GetOption<int>("max-scalarrepl-recursion", 3);
        }

        private void AddInliningPass(PassManager Manager, BodyPassArgument Argument)
        {
            var log = Argument.PassEnvironment.Log;
            const string optionName = "scalarrepl-inline";
            string option = log.Options.GetOption<string>(optionName, "full");

            if (option.Equals("fast", StringComparison.InvariantCultureIgnoreCase))
            {
                // Perform fast inlining
                Manager.RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(FastInliningPass.Instance, FastInliningPass.InliningPassName));
                Manager.RegisterPassCondition(InliningPass.InliningPassName, optInfo => true);
            }
            else if (option.Equals("full", StringComparison.InvariantCultureIgnoreCase))
            {
                // Perform full inlining
                Manager.RegisterMethodPass(new PassInfo<BodyPassArgument, IStatement>(InliningPass.Instance, InliningPass.InliningPassName));
                Manager.RegisterPassCondition(InliningPass.InliningPassName, optInfo => true);
            }
            else if (!option.Equals("none", StringComparison.InvariantCultureIgnoreCase)
                  && !string.IsNullOrWhiteSpace(option)
                  && UnknownInlinerArgumentWarning.UseWarning(log.Options))
            {
                var nodes = new List<MarkupNode>();
                nodes.Add(new MarkupNode(NodeConstants.TextNodeType, "'"));
                nodes.Add(new MarkupNode(NodeConstants.BrightNodeType, "-" + optionName + "=" + option));
                nodes.Add(new MarkupNode(NodeConstants.TextNodeType, "' was not recognized as a known inliner. Use 'full', 'fast' or 'none'. "));
                nodes.Add(UnknownInlinerArgumentWarning.CauseNode);

                log.LogWarning(new LogEntry(
                    "unknown option", nodes));
            }
        }

        public override Func<IStatement, IStatement> GetBodyOptimizer(BodyPassArgument Argument)
        {
            var emptyLog = new EmptyCompilerLog(Argument.PassEnvironment.Log.Options);
            var passManager = new PassManager(PassExtensions.SSAPassManager);
            AddInliningPass(passManager, Argument);

            var optSuite = passManager.CreateSuite(emptyLog);
            var newArgs = new BodyPassArgument(
                new DerivedBodyPassEnvironment(Argument.PassEnvironment, emptyLog), Argument.Metadata,
                Argument.DeclaringMethod, null);
            return body => optSuite.MethodPass.Apply(new BodyPassArgument(newArgs, body));
        }
    }
}
