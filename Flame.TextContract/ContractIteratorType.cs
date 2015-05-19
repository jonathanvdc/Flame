using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractIteratorType : ContractPrimitiveType
    {
        static ContractIteratorType()
        {
            Instance = new ContractIteratorType();
        }

        protected ContractIteratorType()
        {
        }

        public static ContractIteratorType Instance { get; private set; }

        public override string Name
        {
            get { return "iterator"; }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[]
            { 
                new EnumerableAttribute(ContractObjectType.Instance),
                PrimitiveAttributes.Instance.ReferenceTypeAttribute,
                PrimitiveAttributes.Instance.VirtualAttribute 
            };
        }

        public override IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return new ContractIteratorTypeInstance(TypeArguments.Single());
        }

        public override IEnumerable<IGenericParameter> GetGenericParameters()
        {
            var descParam = new DescribedGenericParameter("T", this);
            return new IGenericParameter[] { descParam };
        }
    }

    public class ContractIteratorTypeInstance : ContractPrimitiveType
    {
        public ContractIteratorTypeInstance(IType ElementType)
        {
            this.ElementType = ElementType;
        }

        public IType ElementType { get; private set; }

        public override IType GetGenericDeclaration()
        {
            return ContractIteratorType.Instance;
        }

        public override string Name
        {
            get { return GetGenericDeclaration().Name + "<" + ElementType.Name + ">"; }
        }

        public override string FullName
        {
            get
            {
                return GetGenericDeclaration().FullName + "<" + ElementType.FullName + ">";
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ContractIteratorTypeInstance)
            {
                return ElementType.Equals(((ContractIteratorTypeInstance)obj).ElementType);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return GetGenericDeclaration().GetHashCode() ^ ElementType.GetHashCode();
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] 
            { 
                new EnumerableAttribute(ElementType),
                PrimitiveAttributes.Instance.ReferenceTypeAttribute,
                PrimitiveAttributes.Instance.VirtualAttribute 
            };
        }

        public override IEnumerable<IType> GetGenericArguments()
        {
            return new IType[] { ElementType };
        }

        public override IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return GetGenericDeclaration().GetGenericParameters();
        }

        public override IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return GetGenericDeclaration().MakeGenericType(TypeArguments);
        }
    }
}
