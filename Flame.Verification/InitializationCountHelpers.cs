using Flame.Analysis;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    public static class InitializationCountHelpers
    {
        public static bool IsInitializationNode(INode Value)
        {
            if (Value is ISetVariableNode)
            {
                var setNode = (ISetVariableNode)Value;
				return setNode.Action == VariableNodeAction.Set 
					&& AnalysisHelpers.IsThisVariable(setNode.GetVariable().CreateGetExpression());
            }
            else if (Value is GetMethodExpression)
            {
                var getMethod = (GetMethodExpression)Value;
                return getMethod.Target.IsConstructor && AnalysisHelpers.IsThisVariable(getMethod.Caller);
            }
            else
            {
                return false;
            }
        }

        public static NodeCountVisitor CreateVisitor()
        {
            return new NodeCountVisitor(IsInitializationNode);
        }
    }
}
