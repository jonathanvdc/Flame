using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Optimization;

namespace Flame.Analysis
{
    public abstract class CallAnalysisVisitorBase : AnalysisVisitorBase
    {
        public abstract void Analyze(DissectedCall Call);

        public override bool Analyze(IStatement Value)
        {
            return false;
        }

        public override bool Analyze(IExpression Value)
        {
            var call = DissectedCallHelpers.DissectCall(Value);
            if (call == null)
            {
                return false;   
            }
            else
            {
                Analyze(call);
                return true;
            }
        }
    }
}
