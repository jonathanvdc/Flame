using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractIterableType : ContractPrimitiveType
    {
        static ContractIterableType()
        {
            Instance = new ContractIterableType();            
        }

        protected ContractIterableType()
        {
            typeParam = new DescribedGenericParameter("T", this);
            attrMap = new AttributeMap(new IAttribute[] 
            { 
                new EnumerableAttribute(typeParam), 
                PrimitiveAttributes.Instance.ReferenceTypeAttribute,
                PrimitiveAttributes.Instance.VirtualAttribute
            });
        }

        public static ContractIterableType Instance { get; private set; }

        public override string Name
        {
            get { return "iterable<>"; }
        }

        private IGenericParameter typeParam;

        private readonly AttributeMap attrMap;
        public override AttributeMap Attributes
        {
            get
            {
                return attrMap;
            }
        }

        public override IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[] { typeParam }; }
        }
    }
}
