using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// Defines a type of value.
    /// </summary>
    public interface IType
    {
        /// <summary>
        /// Gets this type's base types. Base types can be either classes or
        /// interfaces.
        /// </summary>
        /// <returns>A read-only list of base types.</returns>
        IReadOnlyList<IType> BaseTypes { get; }
    }
}