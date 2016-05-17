using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractObjectType : ContractPrimitiveType
    {
        static ContractObjectType()
        {
            Instance = new ContractObjectType();
        }

        private ContractObjectType()
        {
        }

        public static ContractObjectType Instance { get; private set; }

        private static readonly AttributeMap attrMap = new AttributeMap(new IAttribute[] 
        { 
            PrimitiveAttributes.Instance.RootTypeAttribute, 
            PrimitiveAttributes.Instance.ReferenceTypeAttribute,
            PrimitiveAttributes.Instance.VirtualAttribute
        });
        public override AttributeMap Attributes
        {
            get
            {
                return attrMap;
            }
        }

        public override UnqualifiedName Name
        {
            get { return new SimpleName("object"); }
        }
    }
}
