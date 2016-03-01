using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public static class AnalysisHelpers
    {
        public static bool IsThisVariable(IExpression Value)
        {
            var innerNode = Value.GetEssentialExpression();
			var varNode = innerNode as IVariableNode;
			if (varNode != null)
			{
				var innerVar = varNode.GetVariable();
				if (innerVar is ThisVariable)
					return true;
				else if (innerVar is AtAddressVariable)
					return IsThisVariable(((AtAddressVariable)innerVar).Pointer);
			}
			return false;
        }

        public static DissectedCall DissectCall(IExpression Node)
        {
            if (Node is InvocationExpression)
            {
                var invExpr = (InvocationExpression)Node;
                var invTrgt = invExpr.Target.GetEssentialExpression();
                if (invTrgt is GetMethodExpression)
                {
                    var target = (GetMethodExpression)invTrgt;
                    return new DissectedCall(target.Caller, target.Target, invExpr.Arguments, target.Op.Equals(Operator.GetVirtualDelegate));
                }
            }
            return null;
        }
    }
}
