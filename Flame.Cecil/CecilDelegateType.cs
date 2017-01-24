using Flame.Cecil.Emit;
using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilDelegateType
    {
        /// <summary>
        /// Checks if the given type is a CLR delegate type.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static bool IsDelegateType(IType Type)
        {
            var bType = Type.GetParent();
            return bType != null && bType.FullName.ToString() == "System.MulticastDelegate";
        }

        /// <summary>
        /// Creates a CLR delegate type from the given type.
        /// </summary>
        /// <param name="Type">Type.</param>
        /// <param name="CodeGenerator">Code generator.</param>
        public static IType Create(IType Type, ICodeGenerator CodeGenerator)
        {
            if (IsDelegateType(Type))
                return Type;
            else
                return CodeGenerator.GetModule().TypeSystem.GetCanonicalDelegate(
                    MethodType.GetMethod(Type));
        }
    }
}
