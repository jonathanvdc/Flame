using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A base type for types that refer to some number of homogeneously-typed
    /// elements.
    /// </summary>
    public abstract class ContainerType : IType
    {
        /// <summary>
        /// Creates a container type from an element type, a name,
        /// a full name and an attribute map.
        /// </summary>
        /// <param name="elementType">The element type.</param>
        /// <param name="name">The container type's name.</param>
        /// <param name="fullName">The container type's fully qualified name.</param>
        /// <param name="attributes">The container type's attributes.</param>
        public ContainerType(
            IType elementType,
            UnqualifiedName name,
            QualifiedName fullName,
            AttributeMap attributes)
        {
            this.ElementType = elementType;
            this.Name = name;
            this.FullName = fullName;
            this.Attributes = attributes;
            this.BaseTypes = new IType[0];
            this.Fields = new IField[0];
            this.Methods = new IMethod[0];
            this.Properties = new IProperty[0];
            this.GenericParameters = new IGenericParameter[0];
        }

        /// <summary>
        /// Gets the type of this container's elements.
        /// </summary>
        /// <returns>The element type.</returns>
        public IType ElementType { get; private set; }

        /// <inheritdoc/>
        public IAssembly Assembly =>
            // TODO: should we maybe return `null` instead?
            // Containers are never "defined" in an assembly.
            // On the other hand, having `ContainerType.Assembly`
            // return `null` feels like a hack.
            ElementType.Assembly;

        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters { get; private set; }

        /// <inheritdoc/>
        public IType ParentType => null;

        /// <inheritdoc/>
        public UnqualifiedName Name { get; private set; }

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes { get; private set; }
    }
}