using dsc.Target;
using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public interface IAssemblyResolver
    {
        Task<IAssembly> ResolveAsync(string Identifier, IDependencyBuilder DependencyBuilder);
        Task CopyAsync(string SourceIdentifier, string TargetIdentifier);
    }
}
