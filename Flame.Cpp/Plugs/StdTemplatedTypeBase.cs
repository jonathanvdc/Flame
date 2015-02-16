using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public abstract class StdTemplatedTypeBase : PrimitiveBase
    {
        public override INamespace DeclaringNamespace
        {
            get { return StdNamespace.Instance; }
        }

        public abstract override IEnumerable<IGenericParameter> GetGenericParameters();
    }
}
