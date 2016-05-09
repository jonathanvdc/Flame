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
            get { return new Version(1, 0, 0, 0); }
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

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public string Name
        {
            get { return "RuntimeRT"; }
        }
    }
}
