using System.Collections.Generic;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A Flame type that wraps an IL type definition.
    /// </summary>
    public sealed class ClrTypeDefinition : IType
    {
        /// <summary>
        /// Creates a Flame type that wraps a particular type
        /// definition.
        /// </summary>
        /// <param name="definition">The definition to wrap.</param>
        /// <param name="assembly">
        /// The assembly that directly defines this type.
        /// </param>
        public ClrTypeDefinition(
            TypeDefinition definition,
            ClrAssembly assembly)
        {
            this.Definition = definition;
            this.Assembly = assembly;
            this.Parent = new TypeParent(assembly);
        }

        /// <summary>
        /// Creates a Flame type that wraps a particular nested type
        /// definition.
        /// </summary>
        /// <param name="definition">The definition to wrap.</param>
        /// <param name="parentType">
        /// The type that directly defines this type.
        /// </param>
        public ClrTypeDefinition(
            TypeDefinition definition,
            ClrTypeDefinition parentType)
        {
            this.Definition = definition;
            this.Assembly = parentType.Assembly;
            this.Parent = new TypeParent(parentType);
        }

        /// <summary>
        /// Gets the assembly that directly or indirectly defines
        /// this type.
        /// </summary>
        /// <returns>The assembly.</returns>
        public ClrAssembly Assembly { get; private set; }

        /// <summary>
        /// Gets the type definition this type is based on.
        /// </summary>
        /// <returns>The type definition.</returns>
        public TypeDefinition Definition { get; private set; }

        /// <inheritdoc/>
        public TypeParent Parent { get; private set; }

        public IReadOnlyList<IType> BaseTypes => throw new System.NotImplementedException();

        public IReadOnlyList<IField> Fields => throw new System.NotImplementedException();

        public IReadOnlyList<IMethod> Methods => throw new System.NotImplementedException();

        public IReadOnlyList<IProperty> Properties => throw new System.NotImplementedException();

        public IReadOnlyList<IType> NestedTypes => throw new System.NotImplementedException();

        public IReadOnlyList<IGenericParameter> GenericParameters => throw new System.NotImplementedException();

        public UnqualifiedName Name => throw new System.NotImplementedException();

        public QualifiedName FullName => throw new System.NotImplementedException();

        public AttributeMap Attributes => throw new System.NotImplementedException();
    }
}