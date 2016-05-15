using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonIteratorType : PythonPrimitiveType
    {
        static PythonIteratorType()
        {
            Instance = new PythonIteratorType();
        }

        protected PythonIteratorType()
        {
        }

        public static PythonIteratorType Instance { get; private set; }

        public override UnqualifiedName Name
        {
            get { return new SimpleName("iterator"); }
        }

        private static readonly AttributeMap attrMap = new AttributeMap(new IAttribute[] 
        { 
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
    }
}
