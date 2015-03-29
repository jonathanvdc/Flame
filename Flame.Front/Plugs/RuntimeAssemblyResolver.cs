using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Plugs
{
    public class RuntimeAssemblyResolver : IAssemblyResolver
    {
        public RuntimeAssemblyResolver(IAssemblyResolver RuntimeLibraryResolver, IAssemblyResolver PlugResolver, string TargetPlatform)
        {
            this.RuntimeLibraryResolver = RuntimeLibraryResolver;
            this.PlugResolver = PlugResolver;
            this.TargetPlatform = TargetPlatform;
        }

        public string TargetPlatform { get; private set; }
        public IAssemblyResolver RuntimeLibraryResolver { get; private set; }
        public IAssemblyResolver PlugResolver { get; private set; }

        public async Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
        {
            var result = await RuntimeLibraryResolver.ResolveAsync(Identifier, DependencyBuilder);
            if (result == null)
            {
                var plugAsm = await PlugHandler.GetPlugAssemblyAsync(PlugResolver, TargetPlatform, Identifier.Path);
                if (plugAsm == null && FlameAssemblies.IsFlameAssembly(Identifier.Path))
                {
                    return await FlameAssemblies.GetFlameAssemblyAsync(PlugResolver, Identifier.Path);
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

        public async Task CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier)
        {
            
        }
    }
}
