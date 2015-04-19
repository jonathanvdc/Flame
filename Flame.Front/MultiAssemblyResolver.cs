using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public class MultiAssemblyResolver : IAssemblyResolver
    {
        public MultiAssemblyResolver()
        {
            resolvers = new Dictionary<string, IAssemblyResolver>(StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, IAssemblyResolver> resolvers;

        public void RegisterResolver(IAssemblyResolver Resolver, params string[] Extensions)
        {
            foreach (var item in Extensions)
            {
                resolvers[item] = Resolver;
            }
        }
        public IAssemblyResolver GetResolver(string Extension)
        {
            if (resolvers.ContainsKey(Extension))
            {
                return resolvers[Extension];
            }
            else
            {
                return null;
            }
        }
        public bool CanResolve(string Extension)
        {
            return resolvers.ContainsKey(Extension);
        }

        public Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
        {
            string ext = Identifier.Extension;
            var resolver = GetResolver(ext);
            return resolver.ResolveAsync(Identifier, DependencyBuilder);
        }

        public Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
        {
            string ext = SourceIdentifier.Extension;
            var resolver = GetResolver(ext);
            if (resolver == null)
            {
                return Task.FromResult<PathIdentifier?>(null);
            }
            return resolver.CopyAsync(SourceIdentifier, TargetIdentifier, Log);
        }
    }
}
