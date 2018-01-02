using System;
using System.Collections.Generic;

namespace Flame.TypeSystem
{
    public abstract class GenericTypeBase : IType
    {
        public GenericTypeBase(IType declaration)
        {
            this.Declaration = declaration;
        }

        public IType Declaration { get; private set; }

        /// <inheritdoc/>
        public abstract TypeParent Parent { get; }

        /// <inheritdoc/>
        public abstract override bool Equals(object obj);

        /// <inheritdoc/>
        public abstract override int GetHashCode();

        public UnqualifiedName Name => throw new System.NotImplementedException();

        public QualifiedName FullName => throw new System.NotImplementedException();

        public IReadOnlyList<IType> BaseTypes => throw new System.NotImplementedException();

        public IReadOnlyList<IField> Fields => throw new System.NotImplementedException();

        public IReadOnlyList<IMethod> Methods => throw new System.NotImplementedException();

        public IReadOnlyList<IProperty> Properties => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public AttributeMap Attributes => Declaration.Attributes;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters =>
            Declaration.GenericParameters;
    }

    public sealed class GenericType : GenericTypeBase
    {
        internal GenericType(
            IType declaration,
            IReadOnlyList<IType> genericArguments)
            : base(declaration)
        {
            this.GenericArguments = genericArguments;
        }

        public IReadOnlyList<IType> GenericArguments { get; private set; }

        /// <inheritdoc/>
        public override TypeParent Parent => Declaration.Parent;

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    public sealed class GenericInstanceType : GenericTypeBase
    {
        internal GenericInstanceType(
            IType declaration,
            GenericTypeBase parentType)
            : base(declaration)
        {
            this.ParentType = parentType;
        }

        /// <summary>
        /// Gets the parent type of this generic instance type.
        /// </summary>
        /// <returns>The parent type.</returns>
        public GenericTypeBase ParentType { get; private set; }

        /// <inheritdoc/>
        public override TypeParent Parent => new TypeParent(ParentType);

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}