using System;
using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A type visitor that replaces types according to a
    /// dictionary.
    /// </summary>
    public sealed class TypeMappingVisitor : TypeVisitor
    {
        /// <summary>
        /// Creates a type mapping visitor from a dictionary.
        /// </summary>
        /// <param name="mapping">
        /// A mapping of source types to target types.
        /// </param>
        public TypeMappingVisitor(IReadOnlyDictionary<IType, IType> mapping)
        {
            this.Mapping = mapping;
        }

        /// <summary>
        /// Gets a mapping of source types to target types for
        /// this type mapping visitor.
        /// </summary>
        /// <returns>A mapping.</returns>
        public IReadOnlyDictionary<IType, IType> Mapping { get; private set; }

        /// <inheritdoc/>
        protected override bool IsOfInterest(IType type)
        {
            return Mapping.ContainsKey(type);
        }

        /// <inheritdoc/>
        protected override IType VisitInteresting(IType type)
        {
            return Mapping[type];
        }
    }

    /// <summary>
    /// A type visitor that uses a type-to-type mapping function under
    /// the hood.
    /// </summary>
    public sealed class TypeFuncVisitor : TypeVisitor
    {
        /// <summary>
        /// Creates a type visitor based on a type-to-type mapping
        /// function.
        /// </summary>
        /// <param name="mapType">
        /// A type-to-type mapping function.
        /// </param>
        public TypeFuncVisitor(Func<IType, IType> mapType)
        {
            this.MapType = mapType;
        }

        /// <summary>
        /// Gets the type-to-type mapping this visitor uses under the hood.
        /// </summary>
        /// <returns>A type-to-type mapping.</returns>
        public Func<IType, IType> MapType { get; private set; }

        /// <inheritdoc/>
        protected override bool IsOfInterest(IType type)
        {
            return true;
        }

        /// <inheritdoc/>
        protected override IType VisitInteresting(IType type)
        {
            return VisitUninteresting(MapType(type));
        }
    }
}
