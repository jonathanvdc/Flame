using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonIterableType : PythonPrimitiveType
    {
        static PythonIterableType()
        {
            Instance = new PythonIterableType();
        }

        protected PythonIterableType()
        {
        }

        public static PythonIterableType Instance { get; private set; }

        public override string Name
        {
            get { return "iterable"; }
        }

        private static readonly Lazy<AttributeMap> attrMap = new Lazy<AttributeMap>(() => new AttributeMap(new IAttribute[]
        {
            new EnumerableAttribute(PythonObjectType.Instance), 
            PrimitiveAttributes.Instance.ReferenceTypeAttribute,
            PrimitiveAttributes.Instance.VirtualAttribute
        }));
        public override AttributeMap Attributes
        {
            get
            {
                return attrMap.Value;
            }
        }
    }
}
