using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;
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
            this.contentsInitializer = Assembly
                .CreateSynchronizedInitializer(AnalyzeContents);

            this.FullName = fullName;
            this.nestedTypeCache = Assembly
                .CreateSynchronizedLazy<IReadOnlyList<IType>>(() =>
            {
                return definition.NestedTypes
                    .Select(t => new ClrTypeDefinition(t, this))
                    .ToArray();
            });
            this.genericParamCache = Assembly
                .CreateSynchronizedLazy<IReadOnlyList<IGenericParameter>>(() =>
            {
                return definition.GenericParameters
                    .Skip(
                        parent.IsType
                            ? parent.TypeOrNull.GenericParameters.Count
                            : 0)
                    .Select(param => new ClrGenericParameter(param, this))
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
        private IReadOnlyList<IField> fieldDefList;
        private Lazy<IReadOnlyList<IGenericParameter>> genericParamCache;
        private Lazy<IReadOnlyList<IType>> nestedTypeCache;
        private AttributeMap attributeMap;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

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
        public IReadOnlyList<IType> BaseTypes
        {
            get
            {
                contentsInitializer.Initialize();
                return baseTypeList;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields
        {
            get
            {
                contentsInitializer.Initialize();
                return fieldDefList;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes => nestedTypeCache.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => genericParamCache.Value;

        private void AnalyzeContents()
        {
            // Analyze attributes.
            var attrBuilder = new AttributeMapBuilder();
            if (!Definition.IsValueType)
            {
                attrBuilder.Add(FlagAttribute.ReferenceType);
            }
            // TODO: support more attributes.
            attributeMap = new AttributeMap(attrBuilder);

            // Analyze base types and interface implementations.
            baseTypeList = (Definition.BaseType == null
                ? new TypeReference[] { }
                : new[] { Definition.BaseType })
                .Concat(Definition.Interfaces.Select(impl => impl.InterfaceType))
                .Select(Assembly.Resolve)
                .ToArray();

            // Analyze fields.
            fieldDefList = Definition.Fields
                .Select(field => new ClrFieldDefinition(field, this))
                .ToArray();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName.ToString();
        }
    }
}
