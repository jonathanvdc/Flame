using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A Flame type that wraps an IL generic parameter.
    /// </summary>
    public sealed class ClrGenericParameter : IGenericParameter
    {
        /// <summary>
        /// Creates a Flame type definition that wraps
        /// around an IL generic parameter.
        /// </summary>
        /// <param name="definition">
        /// The IL type definition to wrap.
        /// </param>
        /// <param name="parentType">
        /// The parent type that defines the generic parameter.
        /// </param>
        public ClrGenericParameter(
            GenericParameter definition,
            ClrTypeDefinition parentType)
            : this(
                definition,
                parentType.Assembly,
                new TypeParent(parentType))
        { }

        /// <summary>
        /// Creates a Flame type definition that wraps
        /// around an IL generic parameter.
        /// </summary>
        /// <param name="definition">
        /// The IL type definition to wrap.
        /// </param>
        /// <param name="parentMethod">
        /// The parent method that defines the generic parameter.
        /// </param>
        public ClrGenericParameter(
            GenericParameter definition,
            ClrMethodDefinition parentMethod)
            : this(
                definition,
                parentMethod.ParentType.Assembly,
                new TypeParent(parentMethod))
        { }

        /// <summary>
        /// Creates a Flame type definition that wraps
        /// around an IL generic parameter.
        /// </summary>
        /// <param name="definition">
        /// The IL type definition to wrap.
        /// </param>
        /// <param name="parentType">
        /// The parent type that defines the generic parameter.
        /// </param>
        private ClrGenericParameter(
            GenericParameter definition,
            ClrGenericParameter parentType)
            : this(
                definition,
                parentType.Assembly,
                new TypeParent(parentType))
        { }

        private ClrGenericParameter(
            GenericParameter definition,
            ClrAssembly assembly,
            TypeParent parent)
        {
            this.Definition = definition;
            this.Assembly = assembly;
            this.Parent = parent;
            this.FullName = new SimpleName(definition.Name)
                .Qualify(parent.Member.FullName);
            this.contentsInitializer = Assembly.CreateSynchronizedInitializer(
                AnalyzeContents);
        }

        /// <summary>
        /// Gets the assembly that directly or indirectly defines
        /// this type.
        /// </summary>
        /// <returns>The assembly.</returns>
        public ClrAssembly Assembly { get; private set; }

        /// <summary>
        /// Gets the generic parameter this type is based on.
        /// </summary>
        /// <returns>The generic parameter.</returns>
        public GenericParameter Definition { get; private set; }

        /// <inheritdoc/>
        public TypeParent Parent { get; private set; }

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public IGenericMember ParentMember =>
            Parent.TypeOrNull ?? (IGenericMember)Parent.Method;

        private DeferredInitializer contentsInitializer;
        private IReadOnlyList<IType> baseTypeList;
        private IReadOnlyList<IGenericParameter> genericParameterList;
        private AttributeMap attributeMap;

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields => EmptyArray<IField>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods => EmptyArray<IMethod>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties => EmptyArray<IProperty>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes => EmptyArray<IType>.Value;

        public IReadOnlyList<IType> BaseTypes
        {
            get
            {
                contentsInitializer.Initialize();
                return baseTypeList;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters
        {
            get
            {
                contentsInitializer.Initialize();
                return genericParameterList;
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

        private void AnalyzeContents()
        {
            baseTypeList = Definition.Constraints
                .Select(Assembly.Resolve)
                .ToArray();

            genericParameterList = Definition.GenericParameters
                .Skip(ParentMember.GenericParameters.Count)
                .Select(param => new ClrGenericParameter(param, this))
                .ToArray();

            var attrBuilder = new AttributeMapBuilder();
            // TODO: analyze other constraints, custom attributes.
            if (Definition.HasReferenceTypeConstraint)
            {
                attrBuilder.Add(FlagAttribute.ReferenceType);
            }
            attributeMap = new AttributeMap(attrBuilder);
        }
    }
}
