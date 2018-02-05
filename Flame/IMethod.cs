using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// Defines a common interface for methods.
    /// </summary>
    public interface IMethod : ITypeMember, IGenericMember
    {
        /// <summary>
        /// Indicates if this method is a constructor.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this method is a constructor; otherwise, <c>false</c>.
        /// </returns>
        bool IsConstructor { get; }

        /// <summary>
        /// Tells if this is a static method. Non-static methods take
        /// a non-null pointer to their parent type as an implicit
        /// first arguments. Static methods do not.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this method is a static method; otherwise, <c>false</c>.
        /// </returns>
        bool IsStatic { get; }

        /// <summary>
        /// Gets the method's return parameter.
        /// </summary>
        Parameter ReturnParameter { get; }

        /// <summary>
        /// Gets the method's parameters.
        /// </summary>
        IReadOnlyList<Parameter> Parameters { get; }

        /// <summary>
        /// Gets the method's base methods.
        /// </summary>
        IReadOnlyList<IMethod> BaseMethods { get; }
    }
}