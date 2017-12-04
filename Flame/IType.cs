using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// Defines a type of value.
    /// </summary>
    public interface IType : ITypeMember, IGenericMember
    {
        /// <summary>
        /// Gets the assembly that declares and owns this type.
        /// This property returns the parent assembly even if
        /// this type is declared inside of another type.
        /// </summary>
        /// <returns>The parent assembly.</returns>
        IAssembly Assembly { get; }

        /// <summary>
        /// Gets this type's base types. Base types can be either classes or
        /// interfaces.
        /// </summary>
        /// <returns>A read-only list of base types.</returns>
        IReadOnlyList<IType> BaseTypes { get; }

        /// <summary>
        /// Gets this type's fields.
        /// </summary>
        /// <returns>A read-only list of fields.</returns>
        IReadOnlyList<IField> Fields { get; }

        /// <summary>
        /// Gets this type's methods.
        /// </summary>
        /// <returns>A read-only list of methods.</returns>
        IReadOnlyList<IMethod> Methods { get; }

        /// <summary>
        /// Gets this type's properties.
        /// </summary>
        /// <returns>A read-only list of properties.</returns>
        IReadOnlyList<IProperty> Properties { get; }
    }
}