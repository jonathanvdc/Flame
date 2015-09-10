using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// Defines common functionality for recompilation strategies:
    /// objects that select which members will definitely be 
    /// recompiled. All other members will only be recompiled
    /// if they are referenced from these "root" members.
    /// </summary>
    public interface IRecompilationStrategy
    {
        /// <summary>
        /// Gets the given assembly's recompilation "roots",
        /// which the assembly recompiler will definitely recompile,
        /// along with any other members that are referenced by them.
        /// Unrelated members will be discarded by the recompiler.
        /// </summary>
        /// <param name="Assembly"></param>
        /// <returns></returns>
        IEnumerable<IMember> GetRoots(IAssembly Assembly);
    }
}
