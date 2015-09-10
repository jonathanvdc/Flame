using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A recompilation strategy where no members are marked as roots.
    /// This is useful when statically linking libraries into an
    /// executable, in which case the recompiler will act as if the 
    /// entry point were a root member (this last statement is true for any executable,
    /// regardless of recompilation strategy).
    /// </summary>
    public sealed class EmptyRecompilationStrategy : IRecompilationStrategy
    {
        private EmptyRecompilationStrategy()
        { }

        static EmptyRecompilationStrategy()
        {
            Instance = new EmptyRecompilationStrategy();
        }

        public static EmptyRecompilationStrategy Instance { get; private set; }

        public IEnumerable<IMember> GetRoots(IAssembly Assembly)
        {
            return Enumerable.Empty<IMember>();
        }
    }
}
