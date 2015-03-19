using Flame.Compiler;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppInvariantExtensions
    {
        public static TypeInvariants GetTypeInvariants(this IType Type)
        {
            if (Type is CppType)
            {
                return ((CppType)Type).Invariants;
            }
            else
            {
                return null;
            }
        }

        public static IMethod GetInvariantsCheckMethod(this IType Type)
        {
            var invars = Type.GetTypeInvariants();
            if (invars == null || !invars.HasAnyInvariants)
            {
                return null;
            }
            return invars.CheckInvariantsMethod;
        }

        public static ICppBlock CreateInvariantsCheck(this IType Type, ICodeGenerator CodeGenerator)
        {
            var checkMethod = Type.GetInvariantsCheckMethod();
            if (checkMethod == null)
            {
                return null;
            }
            var call = CodeGenerator.EmitInvocation(checkMethod, CodeGenerator.GetThis().CreateGetExpression().Emit(CodeGenerator), Enumerable.Empty<ICodeBlock>());
            return (ICppBlock)call;
        }
    }
}