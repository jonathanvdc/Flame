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

        public override string Name
        {
            get { return "std"; }
        }

        #region Static

        static StdNamespace()
        {
            inst = new StdNamespace();

            inst.Register(StdVectorType.Instance);
            inst.Register(StdSharedPointer.Instance);
            inst.Register(StdString.Instance);
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
