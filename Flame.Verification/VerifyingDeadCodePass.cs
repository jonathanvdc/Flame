using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public class VerifyingDeadCodePass : IPass<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>
    {
        public VerifyingDeadCodePass(ICompilerLog Log, string WarningMessage, bool ShowWarning, string UnreachableWarningMessage, bool ShowUnreachableWarning)
        {
            this.Log = Log;
            this.ReturnWarningMessage = WarningMessage;
            this.ShowReturnWarning = ShowWarning;
            this.UnreachableWarningMessage = UnreachableWarningMessage;
            this.ShowUnreachableWarning = ShowUnreachableWarning;
        }

        public ICompilerLog Log { get; private set; }
        public string ReturnWarningMessage { get; private set; }
        public bool ShowReturnWarning { get; private set; }
        public string UnreachableWarningMessage { get; private set; }
        public bool ShowUnreachableWarning { get; private set; }

        public Tuple<IStatement, IMethod> Apply(Tuple<IStatement, IMethod> Value)
        {
            var stmt = Value.Item1;
            var method = Value.Item2;

            var visitor = new DeadCodeVisitor();
            var optStmt = visitor.Visit(stmt);
            if (visitor.CurrentFlow && ShowReturnWarning)
            {
                Log.LogWarning(new LogEntry("Missing return statement?", ReturnWarningMessage, method.GetSourceLocation()));
            }
            if (visitor.RemovedDeadCode && ShowUnreachableWarning)
            {
                Log.LogWarning(new LogEntry("Removed dead code", UnreachableWarningMessage, method.GetSourceLocation()));
            }
            return new Tuple<IStatement, IMethod>(optStmt, method);
        }
    }
}
