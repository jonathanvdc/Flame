namespace Flame
{
    /// <summary>
    /// The root interface for members: constructs that
    /// have a name, a full name and a set of attributes.
    /// </summary>
    public interface IMember
    {
        /// <summary>
        /// Gets the member's full name.
        /// </summary>
        QualifiedName FullName { get; }

        /// <summary>
        /// Gets the member's attributes.
        /// </summary>
        AttributeMap Attributes { get; }
    }

    /// <summary>
    /// Extends members with useful functionality.
    /// </summary>
    public static class MemberExtensions
    {
        /// <summary>
        /// Gets a member's unqualified name.
        /// </summary>
        /// <param name="member">The member to examine.</param>
        /// <returns>The member's unqualified name.</returns>
        public static UnqualifiedName GetName(this IMember member)
        {
            return member.FullName.FullyUnqualifiedName;
        }
    }
}
