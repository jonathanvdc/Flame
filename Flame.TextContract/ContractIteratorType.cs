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
            typeParam = new DescribedGenericParameter("T", this);
        }

        public static ContractIteratorType Instance { get; private set; }

        public override string Name
        {
            get { return "iterator<>"; }
        }

        private IGenericParameter typeParam;

        public override IEnumerable<IAttribute> Attributes
        {
            get
            {
                return new IAttribute[] 
                { 
                    new EnumerableAttribute(typeParam), 
                    PrimitiveAttributes.Instance.ReferenceTypeAttribute,
                    PrimitiveAttributes.Instance.VirtualAttribute
                };
            }
        }

        public override IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[] { typeParam }; }
        }
    }
}
