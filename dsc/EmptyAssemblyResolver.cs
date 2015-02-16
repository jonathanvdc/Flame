using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public class EmptyAssemblyResolver : IAssemblyResolver
    {
        public async Task<IAssembly> ResolveAsync(string Identifier, IDependencyBuilder DependencyBuilder)
        {
            return null;
        }

        public async Task CopyAsync(string SourceIdentifier, string TargetIdentifier)
        {
        }
    }
}
