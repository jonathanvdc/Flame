using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Plugs
{
    public class RuntimeAssemblyResolver : IAssemblyResolver
    {
        public RuntimeAssemblyResolver(IAssemblyResolver RuntimeLibraryResolver, IAssemblyResolver ExternalResolver, string TargetPlatform)
        {
            this.RuntimeLibraryResolver = RuntimeLibraryResolver;
            this.ExternalResolver = ExternalResolver;
            this.TargetPlatform = TargetPlatform;
        }

        public string TargetPlatform { get; private set; }
        public IAssemblyResolver RuntimeLibraryResolver { get; private set; }
        public IAssemblyResolver ExternalResolver { get; private set; }

        public async Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
        {
            var result = await RuntimeLibraryResolver.ResolveAsync(Identifier, DependencyBuilder);
            if (result == null)
            {
                var plugAsm = await PlugHandler.GetPlugAssemblyAsync(ExternalResolver, TargetPlatform, Identifier.Path);
                if (plugAsm == null)
                {
                    if (FlameAssemblies.IsFlameAssembly(Identifier.Path))
                    {
                        return await FlameAssemblies.GetFlameAssemblyAsync(ExternalResolver, Identifier.Path);
                    }
                    else
                    {
                        return await ExternalResolver.ResolveAsync(Identifier, DependencyBuilder);
                    }
                }
                else
                {
                    return plugAsm;
                }
            }
            else
            {
                return result;
            }
        }

        public async Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
        {
            if (FlameAssemblies.IsFlameAssembly(SourceIdentifier.Path))
            {
                var realPath = FlameAssemblies.GetFlameAssemblyPath(SourceIdentifier.Path);
                if (!TargetIdentifier.Extension.Equals(realPath.Extension))
                {
                    TargetIdentifier = TargetIdentifier.AppendExtension(realPath.Extension);
                }
                return await ExternalResolver.CopyAsync(realPath, TargetIdentifier, Log);
            }
            else
            {
                return await ExternalResolver.CopyAsync(SourceIdentifier, TargetIdentifier, Log);
            }
        }
    }
}
