using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A Flame property that wraps an IL property definition.
    /// </summary>
    public sealed class ClrPropertyDefinition : IProperty
    {
        /// <summary>
        /// Creates a wrapper around an IL property definition.
        /// </summary>
        /// <param name="definition">
        /// The definition to wrap in a Flame property.
        /// </param>
        /// <param name="parentType">
        /// The definition's declaring type.
        /// </param>
        public ClrPropertyDefinition(
            PropertyDefinition definition,
            ClrTypeDefinition parentType)
        {
            this.Definition = definition;
            this.ParentType = parentType;
            this.FullName = new SimpleName(definition.Name)
                .Qualify(parentType.FullName);
            this.contentsInitializer = parentType.Assembly
                .CreateSynchronizedInitializer(AnalyzeContents);
        }

        /// <summary>
        /// Gets the IL property definition wrapped by this Flame property.
        /// </summary>
        /// <returns>An IL property definition.</returns>
        public PropertyDefinition Definition { get; private set; }

        /// <summary>
        /// Gets the type that defines this property.
        /// </summary>
        /// <returns>The type that defines this property.</returns>
        public ClrTypeDefinition ParentType { get; private set; }

        private DeferredInitializer contentsInitializer;
        private IType propertyTypeValue;
        private AttributeMap attributeMap;
        private IReadOnlyList<Parameter> indexerParams;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public IType PropertyType
        {
            get
            {
                contentsInitializer.Initialize();
                return propertyTypeValue;
            }
        }

        /// <inheritdoc/>
        public AttributeMap Attributes
        {
            get
            {
                contentsInitializer.Initialize();
                return attributeMap;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<Parameter> IndexerParameters
        {
            get
            {
                contentsInitializer.Initialize();
                return indexerParams;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IAccessor> Accessors
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        /// <inheritdoc/>
        IType ITypeMember.ParentType => ParentType;

        private void AnalyzeContents()
        {
            var assembly = ParentType.Assembly;

            propertyTypeValue = TypeHelpers.BoxIfReferenceType(
                assembly.Resolve(Definition.PropertyType));

            // Analyze the parameter list.
            indexerParams = Definition.Parameters
                .Select(param =>
                    ClrMethodDefinition.WrapParameter(
                        param,
                        assembly,
                        ParentType))
                .ToArray();

            var attrBuilder = new AttributeMapBuilder();
            // TODO: analyze attributes.
            attributeMap = new AttributeMap(attrBuilder);
        }
    }
}