using Flame.Build;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonObjectType : PythonPrimitiveType
    {
        static PythonObjectType()
        {
            Instance = new PythonObjectType();
        }

        private PythonObjectType()
        {
        }

        public static PythonObjectType Instance { get; private set; }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] 
            { 
                PrimitiveAttributes.Instance.RootTypeAttribute, 
                PrimitiveAttributes.Instance.ReferenceTypeAttribute,
                PrimitiveAttributes.Instance.VirtualAttribute
            };
        }

        public override string Name
        {
            get { return "object"; }
        }
    }
}
