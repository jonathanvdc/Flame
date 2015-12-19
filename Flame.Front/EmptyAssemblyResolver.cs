using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public sealed class EmptyAssemblyResolver : IAssemblyResolver
    {
		private EmptyAssemblyResolver() { }

		public static readonly EmptyAssemblyResolver Instance = new EmptyAssemblyResolver();

        public Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
        {
            return Task.FromResult<IAssembly>(null);
        }

        public Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
        {
            return Task.FromResult<PathIdentifier?>(null);
        }
    }
}
