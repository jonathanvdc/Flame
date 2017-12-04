namespace Flame
{
    /// <summary>
    /// Defines a generic parameter.
    /// </summary>
    public interface IGenericParameter : IType
    {
        /// <summary>
        /// Gets the generic member that defines this generic parameter.
        /// </summary>
        IGenericMember ParentMember { get; }

        /// <summary>
        /// Gets the constraint this generic parameter places on types.
        /// </summary>
        TypeConstraint Constraint { get; }
    }
}