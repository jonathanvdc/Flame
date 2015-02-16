using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractGenericInstanceType : IType
    {
        public ContractGenericInstanceType(ContractType GenericDeclaration, IEnumerable<IType> GenericArguments)
        {
            this.GenericDeclaration = GenericDeclaration;
            this.GenericArguments = GenericArguments;
        }

        public ContractType GenericDeclaration { get; private set; }
        public IEnumerable<IType> GenericArguments { get; private set; }

        public IType GetGenericDeclaration()
        {
            return GenericDeclaration;
        }
        public IEnumerable<IType> GetGenericArguments()
        {
            return GenericArguments;
        }

        public string Name
        {
            get
            {
                var genericFreeName = this.GenericDeclaration.GetGenericFreeName();
                StringBuilder sb = new StringBuilder(genericFreeName);
                sb.Append('<');
                bool first = true;
                foreach (var item in GetGenericArguments())
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
        }

        public IContainerType AsContainerType()
        {
            return null;
        }

        public INamespace DeclaringNamespace
        {
            get { return GenericDeclaration.DeclaringNamespace; }
        }

        public IType[] GetBaseTypes()
        {
            return GenericDeclaration.GetBaseTypes();
        }

        public IMethod[] GetConstructors()
        {
            return GenericDeclaration.GetConstructors();
        }

        public IBoundObject GetDefaultValue()
        {
            return GenericDeclaration.GetDefaultValue();
        }

        public IField[] GetFields()
        {
            return GenericDeclaration.GetFields();
        }

        public ITypeMember[] GetMembers()
        {
            return GenericDeclaration.GetMembers();
        }

        public IMethod[] GetMethods()
        {
            return GenericDeclaration.GetMethods();
        }

        public IProperty[] GetProperties()
        {
            return GenericDeclaration.GetProperties();
        }

        public bool IsContainerType
        {
            get { return GenericDeclaration.IsContainerType; }
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new DescribedArrayType(this, Rank);
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return new ContractGenericInstanceType(GenericDeclaration, TypeArguments);
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return GenericDeclaration.GetAttributes();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return GenericDeclaration.GetGenericParameters();
        }
    }
}
