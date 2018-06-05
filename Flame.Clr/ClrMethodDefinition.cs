using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

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

        public Parameter ReturnParameter => throw new System.NotImplementedException();

        public IReadOnlyList<Parameter> Parameters => throw new System.NotImplementedException();

        public IReadOnlyList<IMethod> BaseMethods => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters =>
            genericParameterCache.Value;
        public AttributeMap Attributes => throw new System.NotImplementedException();

        /// <inheritdoc/>
        IType ITypeMember.ParentType => ParentType;
    }
}
