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
        /// Creates a container type from an element type.
        /// </summary>
        /// <param name="elementType">The element type.</param>
        public ContainerType(IType elementType)
        {
            this.ElementType = elementType;
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
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes => throw new System.NotImplementedException();
    }

    /// <summary>
    /// A type for pointers or references to values.
    /// </summary>
    public sealed class PointerType : ContainerType
    {
        public PointerType(IType elementType) : base(elementType)
        {
        }
    }
}