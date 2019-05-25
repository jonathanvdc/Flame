using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A base type for types that refer to some number of homogeneously-typed
    /// elements.
    /// </summary>
    public abstract class ContainerType : IType
    {
        /// <summary>
        /// Creates an uninitialized container type from an element type.
        /// </summary>
        /// <param name="elementType">The element type.</param>
        protected ContainerType(IType elementType)
        {
            this.ElementType = elementType;
        }

        /// <summary>
        /// Creates an initialized container type from an element type,
        /// a name, a full name and an attribute map.
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
            : this(elementType)
        {
            Initialize(name, fullName, attributes);
        }

        /// <summary>
        /// Initializes an uninitialized container type.
        /// </summary>
        /// <param name="name">The container type's name.</param>
        /// <param name="fullName">The container type's fully qualified name.</param>
        /// <param name="attributes">The container type's attributes.</param>
        protected void Initialize(
            UnqualifiedName name,
            QualifiedName fullName,
            AttributeMap attributes)
        {
            this.Name = name;
            this.FullName = fullName;
            this.Attributes = attributes;
        }

        /// <summary>
        /// Gets the type of this container's elements.
        /// </summary>
        /// <returns>The element type.</returns>
        public IType ElementType { get; private set; }

        /// <inheritdoc/>
        public TypeParent Parent => TypeParent.Nothing;

        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes => EmptyArray<IType>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields => EmptyArray<IField>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods => EmptyArray<IMethod>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties => EmptyArray<IProperty>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => EmptyArray<IGenericParameter>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes => EmptyArray<IType>.Value;

        /// <inheritdoc/>
        public UnqualifiedName Name { get; private set; }

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes { get; private set; }

        /// <summary>
        /// Creates a container type that is identical to this one
        /// except for its element type, which is set to a given
        /// type.
        /// </summary>
        /// <param name="newElementType">
        /// The element type of the new container type.
        /// </param>
        /// <returns>Another container type.</returns>
        public abstract ContainerType WithElementType(IType newElementType);

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName.ToString();
        }
    }
}