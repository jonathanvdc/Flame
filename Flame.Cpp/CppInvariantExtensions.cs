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

        public static InvariantMethod GetInvariantsCheckMethod(this IType Type)
        {
            var invars = Type.GetTypeInvariants();
            if (invars == null || !invars.HasAnyInvariants)
            {
                return null;
            }
            return invars.CheckInvariantsMethod;
        }

        public static IMethod GetInvariantsCheckImplementationMethod(this IType Type)
        {
            var invars = Type.GetTypeInvariants();
            if (invars == null || !invars.HasAnyInvariants)
            {
                return null;
            }
            var method = invars.CheckInvariantsImplementationMethod;
            if (method.InlineTestBlock)
            {
                return invars.CheckInvariantsMethod;
            }
            else
            {
                return invars.CheckInvariantsImplementationMethod;
            }
        }

        public static CppField GetIsCheckingInvariantsField(this IType Type)
        {
            var invars = Type.GetTypeInvariants();
            if (invars == null || !invars.HasAnyInvariants)
            {
                return null;
            }
            return invars.IsCheckingInvariantsField;
        }

        public static ICppBlock CreateInvariantsCheck(this IType Type, ICodeGenerator CodeGenerator)
        {
            var invars = Type.GetTypeInvariants();
            if (invars == null || !invars.HasAnyInvariants)
            {
                return null;
            }
            var checkMethod = invars.CheckInvariantsMethod;
            var call = CodeGenerator.EmitInvocation(checkMethod, CodeGenerator.GetThis().EmitGet(), Enumerable.Empty<ICodeBlock>());
            return (ICppBlock)call;
        }
    }
}