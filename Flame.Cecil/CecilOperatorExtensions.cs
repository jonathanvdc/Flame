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
        public static ICodeBlock EmitOperatorCall(this ICodeGenerator CodeGenerator, IMethod Operator, params ICodeBlock[] Arguments)
        {
            if (Operator.GetParameters().Length == Arguments.Length)
            {
                return CodeGenerator.EmitInvocation(CodeGenerator.EmitMethod(Operator, null), Arguments);
            }
            else
            {
                return CodeGenerator.EmitInvocation(CodeGenerator.EmitMethod(Operator, Arguments[0]), Arguments.Skip(1));
            }
        }
    }
}
