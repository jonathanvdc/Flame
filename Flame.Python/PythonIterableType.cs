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

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { new EnumerableAttribute(PythonObjectType.Instance), PrimitiveAttributes.Instance.ReferenceTypeAttribute };
        }
    }
}
