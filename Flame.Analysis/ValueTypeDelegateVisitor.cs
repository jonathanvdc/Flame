using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class ValueTypeDelegateVisitor : AnalysisVisitorBase
    {
        public ValueTypeDelegateVisitor(ICompilerLog Log)
        {
            this.Log = Log;
        }

        public ICompilerLog Log { get; private set; }

        public const string ValueTypeDelegatePassName = "struct-delegate";

        public static readonly WarningDescription ValueTypeDelegateWarning = new WarningDescription(ValueTypeDelegatePassName, Warnings.Instance.All);

        public override bool Analyze(IStatement Value)
        {
            return false;
        }

        private void AnalyzeDelegate(GetMethodExpression Expression)
        {
            if (Expression.Caller == null)
            {
                return;
            }

            var callerType = Expression.Caller.Type;
            if (callerType.get_IsPointer() && callerType.AsContainerType().ElementType.get_IsValueType())
            {
                // There's some shady business going on right here.

                Log.LogWarning(new LogEntry(
                    "Delegate to value type pointer",
                    ValueTypeDelegateWarning.CreateMessage(
                        "A delegate is created that takes a pointer to a value type as its closure. " +
                        "This is dangerous, because the pointer's target may go out of scope before the delegate does. "),
                    CurrentLocation));
            }

            Visit(Expression.Caller);
        }

        public override bool Analyze(IExpression Value)
        {
            if (Value is InvocationExpression)
            {
                var invExpr = (InvocationExpression)Value;
                var essentialExpr = invExpr.Target.GetEssentialExpression();
                if (essentialExpr is GetMethodExpression)
                {
                    var innerExpr = (GetMethodExpression)essentialExpr;
                    Visit(innerExpr.Caller); // 'tis okay
                }
                this.VisitAll(invExpr.Arguments);
                return true; // We got this
            }
            else if (Value is GetMethodExpression)
            {
                AnalyzeDelegate((GetMethodExpression)Value);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
