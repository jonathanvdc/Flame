namespace Flame
{
    /// <summary>
    /// Describes a field: a type member that stores some data.
    /// </summary>
    public interface IField : ITypeMember
    {
        /// <summary>
        /// Tells if this field is static. The storage for static fields
        /// is shared by the entire application, whereas the storage for
        /// instance (i.e., non-static) fields is specific to an instance
        /// of a type.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this field is static; otherwise, <c>false</c>.
        /// </returns>
        bool IsStatic { get; }

        /// <summary>
        /// Gets the type of value stored in this field.
        /// </summary>
        /// <returns>The type of value stored in this field.</returns>
        IType FieldType { get; }
    }
}