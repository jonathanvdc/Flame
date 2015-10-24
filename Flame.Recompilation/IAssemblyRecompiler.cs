using Flame.Compiler;
using Flame.Compiler.Build;
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
        /// Adds the given assembly to the list of assemblies to recompile,
        /// with the given recompilation options.
        /// </summary>
        /// <param name="Source"></param>
        void AddAssembly(IAssembly Source, RecompilationOptions Options);

        /// <summary>
        /// Compiles all included assemblies to the target assembly.
        /// </summary>
        Task RecompileAsync();
    }
}
