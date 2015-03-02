using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public sealed class HeaderComparer : IEqualityComparer<IHeaderDependency>
    {
        private HeaderComparer() { }

        private static HeaderComparer inst;
        public static HeaderComparer Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new HeaderComparer();
                }
                return inst;
            }
        }

        public bool Equals(IHeaderDependency x, IHeaderDependency y)
        {
            return x.IsStandard == y.IsStandard && x.HeaderName == y.HeaderName;
        }

        public int GetHashCode(IHeaderDependency obj)
        {
            return obj.HeaderName.GetHashCode();
        }
    }
}
