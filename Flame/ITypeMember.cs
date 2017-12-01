namespace Flame
{
    /// <summary>
    /// Defines a common interface for members
    /// that may be defined inside types.
    /// </summary>
    public interface ITypeMember : IMember
    {
        /// <summary>
        /// Gets the type that defines this member, if any.
        /// </summary>
        /// <returns>The parent type.</returns>
        IType ParentType { get; }
    }
}