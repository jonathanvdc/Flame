using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
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
            : this(
                definition,
                assembly,
                new TypeParent(assembly),
                NameConversion
                    .ParseSimpleName(definition.Name)
                    .Qualify(
                        NameConversion.ParseNamespace(
                            definition.Namespace)))
        { }

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
            : this(
                definition,
                parentType.Assembly,
                new TypeParent(parentType),
                NameConversion
                    .ParseSimpleName(definition.Name)
                    .Qualify(parentType.FullName))
        { }

        private ClrTypeDefinition(
            TypeDefinition definition,
            ClrAssembly assembly,
            TypeParent parent,
            QualifiedName fullName)
        {
            this.Definition = definition;
            this.Assembly = assembly;
            this.Parent = parent;
            this.contentsInitializer = DeferredInitializer.Create(AnalyzeContents);
            this.FullName = fullName;
            this.nestedTypeCache = new Lazy<IReadOnlyList<IType>>(() =>
            {
                return definition.NestedTypes
                    .Select(t => new ClrTypeDefinition(t, this))
                    .ToArray();
            });
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

        private DeferredInitializer contentsInitializer;
        private IReadOnlyList<IType> baseTypeList;
        private Lazy<IReadOnlyList<IType>> nestedTypeCache;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;


        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes
        {
            get
            {
                contentsInitializer.Initialize();
                return baseTypeList;
            }
        }

        public IReadOnlyList<IField> Fields => throw new System.NotImplementedException();

        public IReadOnlyList<IMethod> Methods => throw new System.NotImplementedException();

        public IReadOnlyList<IProperty> Properties => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes => nestedTypeCache.Value;

        public IReadOnlyList<IGenericParameter> GenericParameters => throw new System.NotImplementedException();

        public AttributeMap Attributes => throw new System.NotImplementedException();

        private void AnalyzeContents()
        {
            Assembly.RunSynchronized(() =>
            {
                // Analyze base types and interface implementations.
                baseTypeList = new[] { Definition.BaseType }
                    .Concat(Definition.Interfaces.Select(impl => impl.InterfaceType))
                    .Select(Assembly.Resolve)
                    .ToArray();
            });
        }
    }
}
