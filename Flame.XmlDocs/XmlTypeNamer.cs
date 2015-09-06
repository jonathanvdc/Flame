using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.XmlDocs
{
    public class XmlTypeNamer : TypeNamerBase
    {
        private XmlTypeNamer()
        {

        }

        protected override string MakePointerType(string ElementType, PointerKind Kind)
        {
            return ElementType + (Kind.Equals(PointerKind.TransientPointer) ? "*" : "@");
        }

        protected override string MakeArrayType(string ElementType, int ArrayRank)
        {
            return ElementType + "[" + new string(',', ArrayRank - 1) + "]";
        }

        protected override string MakeVectorType(string ElementType, IReadOnlyList<int> Dimensions)
        {
            return MakeArrayType(ElementType, Dimensions.Count);
        }

        private string ConvertName(string Name, IEnumerable<IGenericParameter> Parameters)
        {
            if (!Parameters.Any())
            {
                return Name;
            }
            else
            {
                return GenericNameExtensions.TrimGenerics(Name) + "`" + Parameters.Count();
            }
        }

        private string ConvertNamespace(INamespace Namespace)
        {
            if (Namespace is IType)
            {
                return Convert(((IType)Namespace).GetGenericDeclaration());
            }
            else
            {
                return Namespace.FullName;
            }
        }

        protected override string ConvertTypeDefault(IType Type)
        {
            return MemberExtensions.CombineNames(ConvertNamespace(Type.DeclaringNamespace), ConvertName(Type.Name, Type.GenericParameters));
        }

        protected override string MakeGenericType(string GenericDeclaration, IEnumerable<string> TypeArguments)
        {
            return XmlDocumentationExtensions.AppendTypeArguments(GenericDeclaration, TypeArguments);
        }

        protected override string ConvertGenericParameter(IGenericParameter Type)
        {
            return Type.Name;
        }

        private static XmlTypeNamer inst;
        public static XmlTypeNamer Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new XmlTypeNamer();
                }
                return inst;
            }
        }
    }
}
