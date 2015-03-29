using Flame;
using Flame.Compiler;
using Flame.Front;
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
            ReferenceResolver = new MultiAssemblyResolver();
            RegisterResolver(new CecilReferenceResolver(), "dll", "exe");
        }

        public static MultiAssemblyResolver ReferenceResolver { get; private set; }

        public static void RegisterResolver(IAssemblyResolver Resolver, params string[] Extensions)
        {
            ReferenceResolver.RegisterResolver(Resolver, Extensions);
        }
        public static IAssemblyResolver GetResolver(string Extension)
        {
            return ReferenceResolver.GetResolver(Extension);
        }
        public static bool CanResolve(string Extension)
        {
            return ReferenceResolver.CanResolve(Extension);
        }

        public static Task<IAssembly> CopyAndResolveAsync(PathIdentifier RelativePath, PathIdentifier CurrentPath, PathIdentifier OutputFolder, IDependencyBuilder DependencyBuilder)
        {
            return ReferenceResolver.CopyAndResolveAsync(RelativePath, CurrentPath, OutputFolder, DependencyBuilder);
        }
        public static Task<IAssembly> ResolveAsync(PathIdentifier RelativePath, PathIdentifier CurrentPath, IDependencyBuilder DependencyBuilder)
        {
            return ReferenceResolver.ResolveAsync(RelativePath, CurrentPath, DependencyBuilder);
        }
    }
}
