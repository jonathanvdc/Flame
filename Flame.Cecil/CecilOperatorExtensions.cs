using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilOperatorExtensions
    {
        public static ICodeBlock EmitOperatorCall(this ICodeGenerator CodeGenerator, IMethod OperatorMethod, params ICodeBlock[] Arguments)
        {
            if (OperatorMethod.GetParameters().Length == Arguments.Length)
            {
                return CodeGenerator.EmitInvocation(CodeGenerator.EmitMethod(OperatorMethod, null, Operator.GetDelegate), Arguments);
            }
            else
            {
                return CodeGenerator.EmitInvocation(CodeGenerator.EmitMethod(OperatorMethod, Arguments[0], OperatorMethod.GetIsVirtual() ? Operator.GetVirtualDelegate : Operator.GetDelegate), Arguments.Skip(1));
            }
        }
    }
}
