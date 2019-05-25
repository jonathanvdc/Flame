using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// Describes a property: a collection of accessors that
    /// manipulate a (virtual) value.
    /// </summary>
    public interface IProperty : ITypeMember
    {
        /// <summary>
        /// Gets this property's type.
        /// </summary>
        /// <returns>The property's type.</returns>
        IType PropertyType { get; }

        /// <summary>
        /// Gets this property's indexer parameters, i.e., an
        /// additional list of parameters that each accessor
        /// takes.
        /// </summary>
        /// <returns>The indexer parameters.</returns>
        IReadOnlyList<Parameter> IndexerParameters { get; }

        /// <summary>
        /// Gets this property's accessors. Each property can
        /// have at most one accessor any given kind.
        /// </summary>
        /// <returns>A read-only list of accessors.</returns>
        IReadOnlyList<IAccessor> Accessors { get; }
    }

    /// <summary>
    /// Describes an accessor.
    /// </summary>
    public interface IAccessor : IMethod
    {
        /// <summary>
        /// Gets this accessor's kind.
        /// </summary>
        /// <returns>The accessor's kind.</returns>
        AccessorKind Kind { get; }

        /// <summary>
        /// Gets this accessor's parent property: the property
        /// that defines it.
        /// </summary>
        /// <returns>The accessor's parent property.</returns>
        IProperty ParentProperty { get; }
    }
}