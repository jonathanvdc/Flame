using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdNamespace : PrimitiveNamespace
    {
        private StdNamespace()
        {
        }

        public override UnqualifiedName Name
        {
            get { return new SimpleName("std"); }
        }

        #region Static

        static StdNamespace()
        {
            inst = new StdNamespace();

            inst.Register(StdInitializerList.Instance);
        }

        private static StdNamespace inst;
        public static StdNamespace Instance
        {
            get
            {
                return inst;
            }
        }

        #endregion
    }
}
