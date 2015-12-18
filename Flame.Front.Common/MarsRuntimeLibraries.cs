using Flame;
using Flame.Compiler;
using Flame.Front;
using Flame.MIPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public static class MarsRuntimeLibraries
    {
        public static readonly IAssemblyResolver Resolver = new MarsRTLibraryResolver();

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
            public Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
            {
                return Task.FromResult<IAssembly>(RevolveRuntimeLibrary(Identifier.Path));
            }

            public Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
            {
                return Task.FromResult<PathIdentifier?>(null);
            }
        }
    }
}
