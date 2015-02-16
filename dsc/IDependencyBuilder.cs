using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public interface IDependencyBuilder
    {
        /// <summary>
        /// Adds a reference to a runtime library.
        /// </summary>
        /// <param name="RuntimeLibrary"></param>
        /// <returns></returns>
        Task AddRuntimeLibraryAsync(string Identifier);
        /// <summary>
        /// Adds a reference to an assembly.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        Task AddReferenceAsync(ReferenceDependency Reference);

        /// <summary>
        /// Creates a binder for all registered dependencies.
        /// </summary>
        /// <returns></returns>
        IBinder CreateBinder();
    }
}
