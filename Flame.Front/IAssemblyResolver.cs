using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public interface IAssemblyResolver
    {
        Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder);
        Task CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier);
    }

    public static class ResolverExtensions
    {
        public static async Task<IAssembly> CopyAndResolveAsync(this IAssemblyResolver Resolver, PathIdentifier RelativePath, PathIdentifier CurrentPath, PathIdentifier OutputFolder, IDependencyBuilder DependencyBuilder)
        {
            var absPath = new PathIdentifier(CurrentPath, RelativePath);
            string fileName = absPath.Name;
            var targetPath = new PathIdentifier(OutputFolder, fileName);
            if (absPath != targetPath)
            {
                await Resolver.CopyAsync(absPath, targetPath);
            }
            return await Resolver.ResolveAsync(targetPath, DependencyBuilder);
        }
        public static Task<IAssembly> ResolveAsync(this IAssemblyResolver Resolver, PathIdentifier RelativePath, PathIdentifier CurrentPath, IDependencyBuilder DependencyBuilder)
        {
            var absPath = new PathIdentifier(CurrentPath, RelativePath);
            return Resolver.ResolveAsync(absPath, DependencyBuilder);
        }
    }
}
