using Flame;
using Flame.Compiler;
using Flame.Front;
using Flame.Front.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public static class ReferenceResolvers
    {
        static ReferenceResolvers()
        {
            ReferenceResolver = new MultiAssemblyResolver();
            RegisterResolver(new CecilReferenceResolver(), "dll", "exe");
            RegisterResolver(new FlameIRReferenceResolver(), "flo", "fir");
        }

        public static MultiAssemblyResolver ReferenceResolver { get; private set; }

        public static void RegisterResolver(IAssemblyResolver Resolver, params string[] Extensions)
        {
            ReferenceResolver.RegisterResolver(Resolver, Extensions);
        }
        public static IAssemblyResolver GetResolver(string Extension)
        {
            return ReferenceResolver.GetResolver(Extension);
        }
        public static bool CanResolve(string Extension)
        {
            return ReferenceResolver.CanResolve(Extension);
        }
    }
}
