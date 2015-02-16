using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Plugs
{
    public class RuntimeAssemblyResolver : IAssemblyResolver
    {
        public RuntimeAssemblyResolver(IAssemblyResolver Resolver, string TargetPlatform)
        {
            this.Resolver = Resolver;
            this.TargetPlatform = TargetPlatform;
        }

        public string TargetPlatform { get; private set; }
        public IAssemblyResolver Resolver { get; private set; }

        public async Task<IAssembly> ResolveAsync(string Identifier, IDependencyBuilder DependencyBuilder)
        {
            var result = await Resolver.ResolveAsync(Identifier, DependencyBuilder);
            if (result == null)
            {
                var plugAsm = await PlugHandler.GetPlugAssemblyAsync(TargetPlatform, Identifier);
                if (plugAsm == null && FlameAssemblies.IsFlameAssembly(Identifier))
                {
                    return await FlameAssemblies.GetFlameAssemblyAsync(Identifier);
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

        public async Task CopyAsync(string SourceIdentifier, string TargetIdentifier)
        {
            
        }
    }
}
