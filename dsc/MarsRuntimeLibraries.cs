using Flame;
using Flame.Compiler;
using Flame.Front;
using Flame.MIPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public static class MarsRuntimeLibraries
    {
        private static IAssemblyResolver resolver;
        public static IAssemblyResolver Resolver
        {
            get
            {
                if (resolver == null)
                {
                    resolver = new MarsRTLibraryResolver();
                }
                return resolver;
            }
        }

        public static IAssembly RevolveRuntimeLibrary(string Identifier)
        {
            switch (Identifier)
            {
                case "PlatformRT":
                case "PortableRT":
                case "mscorlib":
                    return MarsPlatformRT.Instance;
                case "System":
                case "System.Core":
                case "System.Xml":
                default:
                    return null;
            }
        }

        private class MarsRTLibraryResolver : IAssemblyResolver
        {
            public async Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
            {
                return RevolveRuntimeLibrary(Identifier.Path);
            }

            public async Task CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
            {
            }
        }
    }
}
