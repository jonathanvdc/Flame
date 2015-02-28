using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdxNamespace : PrimitiveNamespace
    {
        private StdxNamespace()
        {
        }

        public override string Name
        {
            get { return "stdx"; }
        }

        #region Static

        static StdxNamespace()
        {
            inst = new StdxNamespace();

            inst.Register(StdxArraySlice.Instance);
            inst.Register(StdxFinally.Instance);
        }

        private static StdxNamespace inst;
        public static StdxNamespace Instance
        {
            get
            {
                return inst;
            }
        }

        #endregion
    }
}
