using Flame.Front.Target;
using Flame;
using Flame.Cecil;
using Flame.Compiler;
using Flame.Front;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public static class CecilRuntimeLibraries
    {
        public static readonly IAssemblyResolver Resolver = new CecilRTLibraryResolver();

        public static IAssembly RevolveRuntimeLibrary(string Identifier, IDependencyBuilder DependencyBuilder)
        {
            string asmPath;
            switch (Identifier)
            {
                case "PlatformRT":
                case "PortableRT":
                case "mscorlib":
                    asmPath = typeof(Math).Assembly.Location;
                    break;
                case "System":
                    asmPath = typeof(System.Net.WebRequest).Assembly.Location;
                    break;
                case "System.Core":
                    asmPath = typeof(Enumerable).Assembly.Location;
                    break;
                case "System.Xml":
                    asmPath = typeof(System.Xml.XmlDocument).Assembly.Location;
                    break;
                default:
                    asmPath = MonoGlobalAssemblyCache.ResolvePartialName(Identifier);
                    break;
            }
            if (asmPath == null)
                return null;
            
            var readerParams = DependencyBuilder.GetCecilReaderParameters();
            var asmDef = Mono.Cecil.AssemblyDefinition.ReadAssembly(asmPath, readerParams);
            return new CecilAssembly(asmDef, CecilReferenceResolver.ConversionCache);
        }

        private class CecilRTLibraryResolver : IAssemblyResolver
        {
            public Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
            {
                return Task.FromResult<IAssembly>(RevolveRuntimeLibrary(Identifier.Path, DependencyBuilder));
            }

            public Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
            {
                return Task.FromResult<PathIdentifier?>(null);
            }
        }
    }
}
