using Flame.Collections;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A CLR field definition.
    /// </summary>
    public sealed class ClrFieldDefinition : IField
    {
        /// <summary>
        /// Creates a Flame field definition that wraps
        /// around an IL field definition.
        /// </summary>
        /// <param name="definition">
        /// The IL field definition to wrap.
        /// </param>
        /// <param name="parentType">
        /// The parent type that defines the field wrapper.
        /// </param>
        public ClrFieldDefinition(
            FieldDefinition definition,
            ClrTypeDefinition parentType)
        {
            this.Definition = definition;
            this.ParentType = parentType;
            this.FullName = new SimpleName(definition.Name)
                .Qualify(parentType.FullName);
        }

        /// <summary>
        /// Gets the IL field definition wrapped by this
        /// Flame field definition.
        /// </summary>
        /// <returns>An IL field definition.</returns>
        public FieldDefinition Definition { get; private set; }

        /// <summary>
        /// Gets this field definition's parent type.
        /// </summary>
        /// <returns>The parent type of this field definition.</returns>
        public ClrTypeDefinition ParentType { get; private set; }

        private DeferredInitializer contentsInitializer;
        private bool isStaticValue;
        private IType fieldTypeValue;
        private AttributeMap attributeMap;

        /// <inheritdoc/>
        public bool IsStatic
        {
            get
            {
                contentsInitializer.Initialize();
                return isStaticValue;
            }
        }

        /// <inheritdoc/>
        public IType FieldType
        {
            get
            {
                contentsInitializer.Initialize();
                return fieldTypeValue;
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
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        IType ITypeMember.ParentType => ParentType;

        private void AnalyzeContents()
        {
            ParentType.Assembly.RunSynchronized(() =>
            {
                isStaticValue = Definition.IsStatic;
                fieldTypeValue = TypeHelpers.BoxIfReferenceType(
                    ParentType.Assembly.Resolve(Definition.FieldType));

                var attrBuilder = new AttributeMapBuilder();
                // TODO: analyze attributes.
                attributeMap = new AttributeMap(attrBuilder);
            });
        }
    }
}
