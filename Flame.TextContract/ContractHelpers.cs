using Flame.Compiler;
using Flame.CodeDescription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public static class ContractHelpers
    {
        public static string GetTypeName(IType Type)
        {
            if (Type.GetIsInteger())
            {
                return "integer";
            }
            else if (Type.GetIsFloatingPoint())
            {
                return "float";
            }
            else if (Type.GetIsBit())
            {
                return "bitstring";
            }
            else if (Type.Equals(PrimitiveTypes.String))
            {
                return "string";
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return "boolean";
            }
            else if (Type.Equals(PrimitiveTypes.Char))
            {
                return "char";
            }
            else if (Type.GetIsArray())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(GetTypeName(Type.AsContainerType().ElementType));
                sb.Append('[');
                int len = Type.AsContainerType().AsArrayType().ArrayRank;
                for (int i = 1; i < len; i++)
                {
                    sb.Append(',');
                }
                sb.Append(']');
                return sb.ToString();
            }
            else if (Type.GetIsVector())
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(GetTypeName(Type.AsContainerType().ElementType));
                sb.Append('[');
                var dims = Type.AsContainerType().AsVectorType().Dimensions;
                sb.Append(dims[0]);
                for (int i = 1; i < dims.Count; i++)
                {
                    sb.Append(", ");
                    sb.Append(dims[i]);
                }
                sb.Append(']');
                return sb.ToString();
            }
            else if (Type.GetIsPointer())
            {
                return Type.AsContainerType().ElementType + Type.AsContainerType().AsPointerType().PointerKind.Extension;
            }
            else if (Type.GetIsGenericInstance())
            {
                var decl = Type.GetGenericDeclaration();
                var genericFreeName = decl.GetGenericFreeName();
                StringBuilder sb = new StringBuilder(genericFreeName);
                sb.Append('<');
                bool first = true;
                foreach (var item in Type.GetGenericArguments())
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append(ContractHelpers.GetTypeName(item));
                }
                sb.Append('>');
                return sb.ToString();
            }
            else
            {
                return Type.Name.ToString();
            }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the member belongs in a text contract or not.
        /// </summary>
        /// <param name="Member"></param>
        /// <returns></returns>
        public static bool InContract(IMember Member)
        {
            return Member.Name != null && !Member.Name.ToString().StartsWith("_");
        }

        public static string GetAccessCode(AccessModifier Access)
        {
            switch (Access)
            {
                case AccessModifier.Private:
                    return "-";
                case AccessModifier.Protected:
                case AccessModifier.ProtectedAndAssembly:
                    return "#";
                case AccessModifier.Assembly:
                case AccessModifier.ProtectedOrAssembly:
                case AccessModifier.Public:
                default:
                    return "+";
            }
        }

        public static IReadOnlyList<string> GetModifiers(IMember Member)
        {
            var list = new List<string>();
            if (Member.GetIsConstant())
            {
                list.Add("query");
            }
            return list;
        }

        public static CodeBuilder GetDocumentationCode(this IMember Member)
        {
            return Member.GetDocumentationLineComments("//");
        }
    }
}
