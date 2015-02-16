using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public static class ReferenceResolvers
    {
        static ReferenceResolvers()
        {
            resolvers = new Dictionary<string, IAssemblyResolver>(StringComparer.OrdinalIgnoreCase);
            RegisterResolver(new CecilReferenceResolver(), "dll", "exe");
        }

        private static Dictionary<string, IAssemblyResolver> resolvers;

        public static void RegisterResolver(IAssemblyResolver Resolver, params string[] Extensions)
        {
            foreach (var item in Extensions)
            {
                resolvers[item] = Resolver;
            }
        }
        public static IAssemblyResolver GetResolver(string Extension)
        {
            return resolvers[Extension];
        }
        public static bool CanResolve(string Extension)
        {
            return resolvers.ContainsKey(Extension);
        }
        public static async Task<IAssembly> CopyAndResolveAsync(IDependencyBuilder DependencyBuilder, string RelativePath, string CurrentPath, string OutputFolder)
        {
            string absPath = new Uri(new Uri(CurrentPath), new Uri(RelativePath, UriKind.RelativeOrAbsolute)).AbsolutePath;
            string ext = System.IO.Path.GetExtension(absPath).TrimStart('.');
            string fileName = System.IO.Path.GetFileName(absPath);
            string targetPath = System.IO.Path.Combine(OutputFolder, fileName);
            var resolver = GetResolver(ext);
            if (absPath != targetPath)
            {
                await resolver.CopyAsync(absPath, targetPath);
            }
            return await resolver.ResolveAsync(targetPath, DependencyBuilder);
        }
        public static Task<IAssembly> ResolveAsync(IDependencyBuilder DependencyBuilder, string RelativePath, string CurrentPath)
        {
            string absPath = new Uri(new Uri(CurrentPath), new Uri(RelativePath, UriKind.RelativeOrAbsolute)).AbsolutePath;
            string ext = absPath.Split('.').Last();
            var resolver = GetResolver(ext);
            return resolver.ResolveAsync(absPath, DependencyBuilder);
        }
    }
}
