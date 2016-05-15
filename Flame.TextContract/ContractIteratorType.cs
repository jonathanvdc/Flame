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
            attrMap = new AttributeMap(new IAttribute[] 
            { 
                new EnumerableAttribute(typeParam), 
                PrimitiveAttributes.Instance.ReferenceTypeAttribute,
                PrimitiveAttributes.Instance.VirtualAttribute
            });
        }

        public static ContractIteratorType Instance { get; private set; }

        public override UnqualifiedName Name
        {
            get { return new SimpleName("iterator", 1); }
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
