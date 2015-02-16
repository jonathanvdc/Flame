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

        public override string Name
        {
            get { return "iterator"; }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { PrimitiveAttributes.Instance.ReferenceTypeAttribute };
        }
    }
}
