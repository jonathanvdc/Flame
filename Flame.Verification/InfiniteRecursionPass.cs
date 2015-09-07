using Flame;
using Flame.Analysis;
using Flame.Build;
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
    public class InfiniteRecursionPass : IPass<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>
    {
        public InfiniteRecursionPass(ICompilerLog Log)
        {
            this.Log = Log;
        }

        public ICompilerLog Log { get; private set; }

        public const string InfiniteRecursionWarningName = "infinite-recursion";

        /// <summary>
        /// Gets a boolean value that tells if this pass is useful for the given log,
        /// i.e. not all of its warnings have been suppressed.
        /// </summary>
        /// <param name="Log"></param>
        /// <returns></returns>
        public static bool IsUseful(ICompilerLog Log)
        {
            return Log.UseDefaultWarnings(InfiniteRecursionWarningName);
        }

        public Tuple<IStatement, IMethod> Apply(Tuple<IStatement, IMethod> Value)
        {
            // Count self-calls
            var matchCall = new Func<DissectedCall, bool>(call => 
                call.Method.Equals(Value.Item2) && !call.IsVirtual);

            var visitor = new NodeCountVisitor(NodeCountVisitor.MatchCalls(matchCall));
            visitor.Visit(Value.Item1);

            if (visitor.CurrentFlow.Min > 0)
            {
                // All paths have more than one self-call.
                // Flag infinite recursion.

                var msg = new LogEntry("Infinite recursion",
                                       "Every path in this method's control flow graph contains a call to itself. " +
                                       "Once called, it will never terminate. " +
                                       Warnings.Instance.GetWarningNameMessage(InfiniteRecursionWarningName),
                                       Value.Item2.GetSourceLocation());

                Log.LogWarning(InitializationCountPass.AppendNeutralLocations(msg, visitor, "Self-call: "));
            }

            return Value;
        }
    }
}
