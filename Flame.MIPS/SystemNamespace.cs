using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public sealed class SystemNamespace : INamespaceBranch
    {
        private SystemNamespace()
        { }

        private static SystemNamespace inst;
        public static SystemNamespace Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new SystemNamespace();
                }
                return inst;
            }
        }

        public IEnumerable<IType> Types
        {
            get
            {
                return new IType[]
                {
                    MemorySystemType.Instance
                };
            }
        }

        public IAssembly DeclaringAssembly
        {
            get { return MarsPlatformRT.Instance; }
        }

        public string FullName
        {
            get { return Name; }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public string Name
        {
            get { return "System"; }
        }

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return new INamespaceBranch[0]; }
        }
    }
}
