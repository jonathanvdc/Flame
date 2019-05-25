using System;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A specialization of a property that is obtained by observing
    /// a property of a generic type specialization, direct or otherwise.
    /// </summary>
    public sealed class IndirectPropertySpecialization : IProperty
    {
        private IndirectPropertySpecialization(
            IProperty declaration,
            TypeSpecialization parentType)
        {
            this.Declaration = declaration;
            this.specializedParentType = parentType;
        }

        private static IndirectPropertySpecialization InitializeInstance(
            IndirectPropertySpecialization instance)
        {
            instance.qualName = instance.Declaration.Name.Qualify(
                instance.ParentType.FullName);
            instance.propType = instance.specializedParentType.InstantiatingVisitor.Visit(
                instance.Declaration.PropertyType);
            instance.indexerParamCache = new Lazy<IReadOnlyList<Parameter>>(
                instance.CreateIndexerParameters);
            instance.accessorCache = new Lazy<IReadOnlyList<IAccessor>>(
                instance.CreateAccessors);

            return instance;
        }

        /// <summary>
        /// Gets the property's generic declaration.
        /// </summary>
        /// <returns>The property's declaration.</returns>
        public IProperty Declaration { get; private set; }

        private TypeSpecialization specializedParentType;
        private QualifiedName qualName;
        private IType propType;
        private Lazy<IReadOnlyList<Parameter>> indexerParamCache;
        private Lazy<IReadOnlyList<IAccessor>> accessorCache;

        /// <inheritdoc/>
        public IType PropertyType => propType;

        /// <inheritdoc/>
        public IReadOnlyList<Parameter> IndexerParameters => indexerParamCache.Value;

        private IReadOnlyList<Parameter> CreateIndexerParameters()
        {
            return specializedParentType.InstantiatingVisitor.VisitAll(
                Declaration.IndexerParameters);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IAccessor> Accessors => accessorCache.Value;

        private IReadOnlyList<IAccessor> CreateAccessors()
        {
            var declMethods = Declaration.Accessors;
            var methods = new IAccessor[declMethods.Count];
            for (int i = 0; i < methods.Length; i++)
            {
                methods[i] = IndirectAccessorSpecialization.Create(declMethods[i], this);
            }
            return methods;
        }


        /// <inheritdoc/>
        public IType ParentType => specializedParentType;

        /// <inheritdoc/>
        public UnqualifiedName Name => qualName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public QualifiedName FullName => qualName;

        /// <inheritdoc/>
        public AttributeMap Attributes => Declaration.Attributes;

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName.ToString();
        }

        // This cache interns all indirect property specializations:
        // if two IndirectPropertySpecialization instances (in the wild, not
        // in this private set-up logic) have equal declaration
        // and parent types, then they are *referentially* equal.
        private static InterningCache<IndirectPropertySpecialization> instanceCache
            = new InterningCache<IndirectPropertySpecialization>(
                new StructuralIndirectPropertySpecializationComparer(),
                InitializeInstance);

        /// <summary>
        /// Creates a generic property specialization of a particular generic
        /// property declaration.
        /// </summary>
        /// <param name="declaration">
        /// The generic property declaration that is specialized into
        /// a concrete property.
        /// </param>
        /// <param name="parentType">
        /// A specialization of the generic declaration's parent type.
        /// </param>
        /// <returns>A specialization of the generic declaration.</returns>
        internal static IndirectPropertySpecialization Create(
            IProperty declaration,
            TypeSpecialization parentType)
        {
            return instanceCache.Intern(
                new IndirectPropertySpecialization(declaration, parentType));
        }
    }

    internal sealed class StructuralIndirectPropertySpecializationComparer : IEqualityComparer<IndirectPropertySpecialization>
    {
        public bool Equals(IndirectPropertySpecialization x, IndirectPropertySpecialization y)
        {
            return object.Equals(x.Declaration, y.Declaration)
                && object.Equals(x.ParentType, y.ParentType);
        }

        public int GetHashCode(IndirectPropertySpecialization obj)
        {
            return (((object)obj.Declaration).GetHashCode() << 3)
                ^ ((object)obj.ParentType).GetHashCode();
        }
    }
}