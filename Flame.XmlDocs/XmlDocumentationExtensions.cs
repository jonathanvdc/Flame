using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.XmlDocs
{
    public static class XmlDocumentationExtensions
    {
        public static string GetXmlDocName(this IField Field)
        {
            return "F:" + Field.FullName;
        }

        public static string GetXmlTypeName(this IType Type)
        {
            return XmlTypeNamer.Instance.Convert(Type);
        }

        public static string GetXmlDocName(this IType Type)
        {
            return "T:" + Type.GetXmlTypeName();
        }

        public static string GetXmlDocName(this IProperty Property)
        {
            return "P:" + Property.FullName;
        }

        public static string AppendTypeArguments(string Name, IEnumerable<string> TypeArguments)
        {
            if (!TypeArguments.Any())
            {
                return Name;
            }
            StringBuilder sb = new StringBuilder(Name);
            sb.Append('{');
            sb.Append(TypeArguments.First());
            foreach (var item in TypeArguments.Skip(1))
            {
                sb.Append(',');
                sb.Append(item);
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string GetXmlDocName(this IMethod Method)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("M:");
            sb.Append(Method.DeclaringType.GetXmlTypeName());
            sb.Append(".");
            sb.Append(Method.Name);
            if (Method.get_IsGeneric())
            {
                sb.Append("``");
                sb.Append(Method.GetGenericParameters().Count());
            }
            var paramTypes = Method.GetParameters().GetTypes();
            if (paramTypes.Length > 0)
            {
                sb.Append("(");
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    sb.Append(paramTypes[i].GetXmlTypeName());
                }
                sb.Append(")");
            }
            return sb.ToString();
        }

        public static string GetXmlDocName(this IMember Member)
        {
            if (Member is IMethod)
            {
                return GetXmlDocName((IMethod)Member);
            }
            else if (Member is IProperty)
            {
                return GetXmlDocName((IProperty)Member);
            }
            else if (Member is IField)
            {
                return GetXmlDocName((IField)Member);
            }
            else if (Member is IType)
            {
                return GetXmlDocName((IType)Member);
            }
            else
            {
                return "U:" + Member.FullName;
            }
        }
    }
}
