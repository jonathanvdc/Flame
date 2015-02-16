using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class MarsEnvironment : IEnvironment
    {
        public MarsEnvironment()
        { }

        public IType EnumerableType
        {
            get { return null; }
        }

        public IType EnumeratorType
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MIPS/MARS"; }
        }

        public IType RootType
        {
            get { return null; }
        }
    }
}
