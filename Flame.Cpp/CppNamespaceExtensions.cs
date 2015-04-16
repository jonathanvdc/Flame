using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppNamespaceExtensions
    {
        public static Plugs.StdxNamespace GetStdxNamespace(this ICppEnvironment Envionment)
        {
            return Envionment.StandardNamespaces.OfType<Plugs.StdxNamespace>().FirstOrDefault();
        }
    }
}
