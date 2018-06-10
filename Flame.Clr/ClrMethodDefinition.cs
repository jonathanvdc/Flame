using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Flame.Clr
{
    /// <summary>
    /// A Flame method that wraps an IL method definition.
    /// </summary>
    public sealed class ClrMethodDefinition : IMethod
    {
        /// <summary>
        /// Creates a wrapper around an IL method definition.
        /// </summary>
        /// <param name="definition">
        /// The definition to wrap in a Flame type.
        /// </param>
        /// <param name="parentType">
        /// The definition's declaring type.
        /// </param>
        public ClrMethodDefinition(
            MethodDefinition definition,
            ClrTypeDefinition parentType)
        {
            this.Definition = definition;
            this.ParentType = parentType;
            this.IsConstructor = Definition.IsConstructor;
            this.IsStatic = Definition.IsStatic;

            this.FullName = NameConversion
                .ParseSimpleName(definition.Name)
                .Qualify(parentType.FullName);

            this.genericParameterCache = parentType.Assembly
                .CreateSynchronizedLazy<IReadOnlyList<IGenericParameter>>(() =>
                    definition.GenericParameters
                        .Select(param => new ClrGenericParameter(param, this))
                        .ToArray());

            this.contentsInitializer = parentType.Assembly
                .CreateSynchronizedInitializer(AnalyzeContents);
        }

        /// <summary>
        /// Gets the IL method definition wrapped by this
        /// Flame method definition.
        /// </summary>
        /// <returns>An IL method definition.</returns>
        public MethodDefinition Definition { get; private set; }

        /// <summary>
        /// Gets the type that defines this method.
        /// </summary>
        /// <returns>The type that defines this method.</returns>
        public ClrTypeDefinition ParentType { get; private set; }

        /// <inheritdoc/>
        public bool IsConstructor { get; private set; }

        /// <inheritdoc/>
        public bool IsStatic { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        private Lazy<IReadOnlyList<IGenericParameter>> genericParameterCache;
        private DeferredInitializer contentsInitializer;
        private Parameter returnParam;
        private IReadOnlyList<Parameter> formalParams;
        private IReadOnlyList<IMethod> baseMethods;
        private AttributeMap attributeMap;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters =>
            genericParameterCache.Value;

        /// <inheritdoc/>
        public Parameter ReturnParameter
        {
            get
            {
                contentsInitializer.Initialize();
                return returnParam;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<Parameter> Parameters
        {
            get
            {
                contentsInitializer.Initialize();
                return formalParams;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> BaseMethods => throw new System.NotImplementedException();

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
        IType ITypeMember.ParentType => ParentType;

        private void AnalyzeContents()
        {
            var assembly = ParentType.Assembly;

            // Analyze the return parameter.
            returnParam = WrapReturnParameter(
                Definition.MethodReturnType,
                assembly,
                this);

            // Analyze the parameter list.
            formalParams = Definition.Parameters
                .Select(param => WrapParameter(param, assembly, this))
                .ToArray();

            // Analyze the method definition's attributes.
            var attrBuilder = new AttributeMapBuilder();
            // TODO: actually analyze attributes.
            attributeMap = new AttributeMap(attrBuilder);
        }

        internal static Parameter WrapParameter(
            ParameterDefinition parameter,
            ClrAssembly assembly,
            IGenericMember enclosingMember)
        {
            var attrBuilder = new AttributeMapBuilder();
            // TODO: actually analyze the parameter's attributes.
            return new Parameter(
                TypeHelpers.BoxIfReferenceType(
                    assembly.Resolve(
                        parameter.ParameterType,
                        enclosingMember)),
                parameter.Name,
                new AttributeMap(attrBuilder));
        }

        internal static Parameter WrapReturnParameter(
            MethodReturnType returnParameter,
            ClrAssembly assembly,
            IGenericMember enclosingMember)
        {
            var attrBuilder = new AttributeMapBuilder();
            // TODO: actually analyze the parameter's attributes.
            return new Parameter(
                TypeHelpers.BoxIfReferenceType(
                    assembly.Resolve(
                        returnParameter.ReturnType,
                        enclosingMember)),
                returnParameter.Name,
                new AttributeMap(attrBuilder));
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents
        /// the current <see cref="T:Flame.Clr.ClrMethodDefinition"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents
        /// the current <see cref="T:Flame.Clr.ClrMethodDefinition"/>.
        /// </returns>
        public override string ToString ()
        {
            return Definition.ToString();
        }
    }
}
