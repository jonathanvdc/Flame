using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public interface IAssemblyRecompiler
    {
        /// <summary>
        /// Gets the target assembly.
        /// </summary>
        IAssemblyBuilder TargetAssembly { [Pure] get; }

        /// <summary>
        /// Compiles a functional equivalent of the source assembly to the target assembly.
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Target"></param>
        Task RecompileAsync(IAssembly Source, RecompilationOptions Options);
    }
}
