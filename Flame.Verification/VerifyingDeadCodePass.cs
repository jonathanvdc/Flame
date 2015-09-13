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
    public class VerifyingDeadCodePass : IPass<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>
    {
        public VerifyingDeadCodePass(string WarningMessage, bool ShowWarning, string UnreachableWarningMessage, bool ShowUnreachableWarning)
        {
            this.ReturnWarningMessage = WarningMessage;
            this.ShowReturnWarning = ShowWarning;
            this.UnreachableWarningMessage = UnreachableWarningMessage;
            this.ShowUnreachableWarning = ShowUnreachableWarning;
        }

        public string ReturnWarningMessage { get; private set; }
        public bool ShowReturnWarning { get; private set; }
        public string UnreachableWarningMessage { get; private set; }
        public bool ShowUnreachableWarning { get; private set; }

        public IStatement Apply(Tuple<IStatement, IMethod, ICompilerLog> Value)
        {
            var stmt = Value.Item1;
            var method = Value.Item2;
            var log = Value.Item3;

            var visitor = new DeadCodeVisitor();
            var optStmt = visitor.Visit(stmt);
            if (visitor.CurrentFlow && ShowReturnWarning)
            {
                log.LogWarning(new LogEntry("Missing return statement?", ReturnWarningMessage, method.GetSourceLocation()));
            }
            if (ShowUnreachableWarning)
            {
                var first = visitor.DeadCodeStatements.FirstOrDefault();

                if (first != null)
                {
                    var node = new MarkupNode("entry", new IMarkupNode[]
                    { 
                        new MarkupNode(NodeConstants.TextNodeType, UnreachableWarningMessage),
                        first.Location.CreateDiagnosticsNode(),
                        RedefinitionHelpers.Instance.CreateNeutralDiagnosticsNode("In method: ", method.GetSourceLocation())
                    });

                    log.LogWarning(new LogEntry("Removed dead code", node));
                }
            }
            return optStmt;
        }
    }
}
