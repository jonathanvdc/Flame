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
            else if (innerNode is ReinterpretCastExpression 
                || innerNode is DynamicCastExpression)
                return IsThisVariable(((ConversionExpressionBase)innerNode).Value);
			return false;
        }
    }
}
