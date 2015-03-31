using dsc.Target;
using Flame;
using Flame.Cecil;
using Flame.Front;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public static class CecilRuntimeLibraries
    {
        private static IAssemblyResolver resolver;
        public static IAssemblyResolver Resolver
        {
            get 
            {
                if (resolver == null)
                {
                    resolver = new CecilRTLibraryResolver();
                }
                return resolver;
            }
        }

        public static IAssembly RevolveRuntimeLibrary(string Identifier)
        {
            Assembly loadedAsm;
            switch (Identifier)
            {
                case "PlatformRT":
                case "PortableRT":
                case "mscorlib":
                    loadedAsm = typeof(Math).Assembly;
                    break;
                case "System":
                    loadedAsm = typeof(System.Net.WebRequest).Assembly;
                    break;
                case "System.Core":
                    loadedAsm = typeof(Enumerable).Assembly;
                    break;
                case "System.Xml":
                    loadedAsm = typeof(System.Xml.XmlDocument).Assembly;
                    break;
                default:
                    return null;
            }
            return new CecilAssembly(Mono.Cecil.AssemblyDefinition.ReadAssembly(loadedAsm.Location));
        }

        private class CecilRTLibraryResolver : IAssemblyResolver
        {
            public async Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
            {
                return RevolveRuntimeLibrary(Identifier.Path);
            }

            public async Task CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier)
            {
            }
        }
    }
}
