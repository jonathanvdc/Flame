using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class TypeNameComparer : IEqualityComparer<IType>
    {
        protected TypeNameComparer()
        {

        }

        private static TypeNameComparer inst;
        public static TypeNameComparer Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new TypeNameComparer();
                }
                return inst;
            }
        }

        public bool Equals(IType x, IType y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(IType obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
