using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using Flame.Syntax;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public sealed class VerifyingDeadCodePass : IPass<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>
    {
        private VerifyingDeadCodePass(bool LogMissingReturn)
        {
            this.LogMissingReturn = LogMissingReturn;
        }

        /// <summary>
        /// A boolean that indicates whether this pass will try to emit
        /// missing-return warnings.
        /// </summary>
        public bool LogMissingReturn { get; private set; }

        public static readonly VerifyingDeadCodePass Instance = new VerifyingDeadCodePass(true);
        public static readonly VerifyingDeadCodePass NoMissingReturnInstance = new VerifyingDeadCodePass(false);

        public static readonly WarningDescription MissingReturnWarning = Warnings.Instance.MissingReturnWarning;
        public static readonly WarningDescription DeadCodeWarning = Warnings.Instance.DeadCodeWarning;

        public IStatement Apply(Tuple<IStatement, IMethod, ICompilerLog> Value)
        {
            var stmt = Value.Item1;

            if (GotoFindingVisitor.UsesGoto(stmt))
                return stmt;

            var method = Value.Item2;
            var log = Value.Item3;
            var visitor = new DeadCodeVisitor();
            var optStmt = visitor.Visit(stmt);
            if (LogMissingReturn && visitor.CurrentFlow &&
                MissingReturnWarning.UseWarning(Value.Item3.Options) &&
                !YieldNodeFindingVisitor.UsesYield(stmt))
            {
                log.LogWarning(new LogEntry("missing return statement?",
                    MissingReturnWarning.CreateMessage("this method may not always return or throw. "),
                    method.GetSourceLocation()));
            }
            var firstUnreachable = visitor.DeadCodeStatements.FirstOrDefault();
            if (firstUnreachable != null && DeadCodeWarning.UseWarning(Value.Item3.Options))
            {
                var node = new MarkupNode("entry", new MarkupNode[]
                {
                    DeadCodeWarning.CreateMessage("unreachable code detected and removed. "),
                    firstUnreachable.Location.CreateDiagnosticsNode(),
                    RedefinitionHelpers.Instance.CreateNeutralDiagnosticsNode("In method: ", method.GetSourceLocation())
                });

                log.LogWarning(new LogEntry("removed dead code", node));
            }
            return optStmt;
        }
    }
}
