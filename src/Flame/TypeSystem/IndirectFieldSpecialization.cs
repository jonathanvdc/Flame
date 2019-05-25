using System;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A specialization of a field belonging to a recursively
    /// generic type.
    /// </summary>
    public sealed class IndirectFieldSpecialization : IField
    {
        private IndirectFieldSpecialization(
            IField declaration,
            TypeSpecialization parentType)
        {
            this.Declaration = declaration;
            this.parentTy = parentType;
        }

        private static IndirectFieldSpecialization InitializeInstance(
            IndirectFieldSpecialization field)
        {
            field.FieldType = field.parentTy.InstantiatingVisitor.Visit(
                field.Declaration.FieldType);
            field.FullName = field.Declaration.Name.Qualify(
                field.parentTy.FullName);
            return field;
        }

        /// <summary>
        /// Gets the generic declaration of which this field
        /// is a specialization.
        /// </summary>
        /// <returns>A generic field declaration.</returns>
        public IField Declaration { get; private set; }

        private TypeSpecialization parentTy;

        /// <inheritdoc/>
        public bool IsStatic => Declaration.IsStatic;

        /// <inheritdoc/>
        public IType FieldType { get; private set; }

        /// <inheritdoc/>
        public IType ParentType => parentTy;

        /// <inheritdoc/>
        public UnqualifiedName Name => Declaration.Name;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes => Declaration.Attributes;

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName.ToString();
        }

        // This cache interns all indirect field specializations:
        // if two IndirectFieldSpecialization instances (in the wild, not
        // in this private set-up logic) have equal declaration
        // and parent types, then they are *referentially* equal.
        private static InterningCache<IndirectFieldSpecialization> instanceCache
            = new InterningCache<IndirectFieldSpecialization>(
                new StructuralIndirectFieldSpecializationComparer(),
                InitializeInstance);

        /// <summary>
        /// Creates a generic field specialization of a particular generic
        /// field declaration.
        /// </summary>
        /// <param name="declaration">
        /// The generic field declaration that is specialized into
        /// a concrete field.
        /// </param>
        /// <param name="parentType">
        /// A specialization of the generic declaration's parent type.
        /// </param>
        /// <returns>A specialization of the generic declaration.</returns>
        internal static IndirectFieldSpecialization Create(
            IField declaration,
            TypeSpecialization parentType)
        {
            return instanceCache.Intern(
                new IndirectFieldSpecialization(declaration, parentType));
        }
    }

    internal sealed class StructuralIndirectFieldSpecializationComparer : IEqualityComparer<IndirectFieldSpecialization>
    {
        public bool Equals(IndirectFieldSpecialization x, IndirectFieldSpecialization y)
        {
            return object.Equals(x.Declaration, y.Declaration)
                && object.Equals(x.ParentType, y.ParentType);
        }

        public int GetHashCode(IndirectFieldSpecialization obj)
        {
            return (((object)obj.Declaration).GetHashCode() << 3)
                ^ ((object)obj.ParentType).GetHashCode();
        }
    }
}