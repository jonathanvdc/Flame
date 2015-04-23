using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class DeclarationLocalComparer : IEqualityComparer<LocalDeclaration>
    {
        protected DeclarationLocalComparer()
        {

        }

        static DeclarationLocalComparer()
        {
            Instance = new DeclarationLocalComparer();
        }

        public static DeclarationLocalComparer Instance { get; private set; }

        public bool Equals(LocalDeclaration x, LocalDeclaration y)
        {
            return x.Local == y.Local;
        }

        public int GetHashCode(LocalDeclaration obj)
        {
            return obj.Local.GetHashCode();
        }
    }
}
