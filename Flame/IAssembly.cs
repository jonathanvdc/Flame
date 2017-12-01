using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// Defines a common interface for assemblies: collections
    /// of types.
    /// </summary>
    public interface IAssembly : IMember
    {
        /// <summary>
        /// Gets a list of all top-level types defined in this assembly.
        /// </summary>
        /// <returns>A list of types that are defined in this assembly.</returns>
        IReadOnlyList<IType> Types { get; }
    }
}