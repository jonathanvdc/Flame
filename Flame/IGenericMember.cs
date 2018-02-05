using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// Defines a generic member: a member that has a list of
    /// generic parameters.
    /// </summary>
    public interface IGenericMember : IMember
    {
        /// <summary>
        /// Gets the list of generic parameters for this generic member.
        /// </summary>
        /// <returns>The generic parameters.</returns>
        IReadOnlyList<IGenericParameter> GenericParameters { get; }
    }
}
