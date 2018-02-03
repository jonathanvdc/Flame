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
}
