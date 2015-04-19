using Flame;
using Flame.Compiler;
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
        Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log);
    }

    public static class ResolverExtensions
    {
        public static async Task<IAssembly> CopyAndResolveAsync(this IAssemblyResolver Resolver, PathIdentifier SourcePath, PathIdentifier OutputFolder, IDependencyBuilder DependencyBuilder)
        {
            string fileName = SourcePath.Name;
            var targetPath = new PathIdentifier(OutputFolder, fileName);
            if (SourcePath != targetPath)
            {
                var result = await Resolver.CopyAsync(SourcePath, targetPath, DependencyBuilder.Log);
                if (result.HasValue)
                {
                    return await Resolver.ResolveAsync(result.Value, DependencyBuilder);
                }
            }
            return await Resolver.ResolveAsync(SourcePath, DependencyBuilder);
        }
        public static Task<IAssembly> CopyAndResolveAsync(this IAssemblyResolver Resolver, PathIdentifier RelativePath, PathIdentifier CurrentPath, PathIdentifier OutputFolder, IDependencyBuilder DependencyBuilder)
        {
            var absPath = CurrentPath.GetAbsolutePath(RelativePath);
            return Resolver.CopyAndResolveAsync(absPath, OutputFolder, DependencyBuilder);
        }
        public static Task<IAssembly> ResolveAsync(this IAssemblyResolver Resolver, PathIdentifier RelativePath, PathIdentifier CurrentPath, IDependencyBuilder DependencyBuilder)
        {
            var absPath = CurrentPath.GetAbsolutePath(RelativePath);
            return Resolver.ResolveAsync(absPath, DependencyBuilder);
        }
    }
}
