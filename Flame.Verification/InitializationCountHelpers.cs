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
        private static bool IsThisVariable(IExpression Value)
        {
            var innerNode = Value.GetEssentialExpression();
            return innerNode is IVariableNode && ((IVariableNode)innerNode).GetVariable() is ThisVariable;
        }

        public static bool IsInitializationNode(INode Value)
        {
            if (Value is ISetVariableNode)
            {
                var setNode = (ISetVariableNode)Value;
                return setNode.Action == VariableNodeAction.Set && setNode.GetVariable() is ThisVariable;
            }
            else if (Value is StoreAtAddressStatement)
            {
                var stmt = (StoreAtAddressStatement)Value;
                return IsThisVariable(stmt.Pointer);
            }
            else if (Value is GetMethodExpression)
            {
                var getMethod = (GetMethodExpression)Value;
                return getMethod.Target.IsConstructor && IsThisVariable(getMethod.Caller);
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
