using Flame.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class MarsPlatformRT : IAssembly
    {
        private MarsPlatformRT()
        { }

        private static MarsPlatformRT inst;
        public static MarsPlatformRT Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new MarsPlatformRT();
                }
                return inst;
            }
        }

        public INamespaceBranch MainNamespace
        {
            get
            {
                return SystemNamespace.Instance;
            }
        }

        public Version AssemblyVersion
        {
            get { return new Version(); }
        }

        public IBinder CreateBinder()
        {
            return new NamespaceTreeBinder(MarsEnvironment.Instance, MainNamespace);
        }

        public IMethod GetEntryPoint()
        {
            return null;
        }

        public string FullName
        {
            get { return "RuntimeRT"; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[0]; }
        }

        public string Name
        {
            get { return "RuntimeRT"; }
        }
    }
}
