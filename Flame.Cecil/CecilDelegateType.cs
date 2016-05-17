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
        /// Gets the given CLR delegate type's invoke method.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static IMethod GetInvokeMethod(IType Type)
        {
            return Type.GetMethods().Single(item => item.Name.ToString() == "Invoke" && !item.IsStatic);
        }

        /// <summary>
        /// Gets the given method type's method signature,
        /// taking into account that the given type may be a
        /// CLR delegate type.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        public static IMethod GetDelegateMethod(IType Type)
        {
            var method = MethodType.GetMethod(Type);
            if (method != null)
            {
                return method;
            }
            else if (CecilDelegateType.IsDelegateType(Type))
            {
                return CecilDelegateType.GetInvokeMethod(Type);
            }
            else
            {
                return null;
            }
        }

        public static IType Create(IType Type, ICodeGenerator CodeGenerator)
        {
            if (IsDelegateType(Type))
            {
                return Type;
            }
            return CodeGenerator.GetModule().TypeSystem.GetCanonicalDelegate(GetDelegateMethod(Type));
        }
    }
}
