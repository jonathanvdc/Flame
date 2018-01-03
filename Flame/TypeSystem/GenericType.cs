using System;
using System.Collections.Generic;
using System.Linq;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A base type for generic instance types.
    /// </summary>
    public abstract class GenericTypeBase : IType
    {
        /// <summary>
        /// Creates a generic type from a declaration.
        /// </summary>
        /// <param name="declaration">A declaration.</param>
        internal GenericTypeBase(IType declaration)
        {
            this.Declaration = declaration;
        }

        /// <summary>
        /// Gets the generic type declaration this type instantiates.
        /// </summary>
        /// <returns>The generic type declaration.</returns>
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

    /// <summary>
    /// A generic type that is instantiated with a list of type arguments.
    /// </summary>
    public sealed class GenericType : GenericTypeBase, IEquatable<GenericType>
    {
        internal GenericType(
            IType declaration,
            IReadOnlyList<IType> genericArguments)
            : base(declaration)
        {
            this.GenericArguments = genericArguments;
        }

        /// <summary>
        /// Gets this generic type's list of generic arguments.
        /// </summary>
        /// <returns>The generic arguments.</returns>
        public IReadOnlyList<IType> GenericArguments { get; private set; }

        /// <inheritdoc/>
        public override TypeParent Parent => Declaration.Parent;

        /// <summary>
        /// Checks if this generic type equals another.
        /// </summary>
        /// <param name="other">A generic type.</param>
        /// <returns>
        /// <c>true</c> if the types are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(GenericType other)
        {
            return object.ReferenceEquals(this, other)
                || (object.Equals(Declaration, other.Declaration)
                    && Enumerable.SequenceEqual<IType>(
                        GenericArguments, other.GenericArguments));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is GenericType && Equals((GenericType)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int result = ((object)Declaration).GetHashCode();
            int genericArgCount = GenericArguments.Count;
            for (int i = 0; i < genericArgCount; i++)
            {
                result = (result << 2) ^ ((object)GenericArguments[i]).GetHashCode();
            }
            return result;
        }
    }

    /// <summary>
    /// A type that is defined in an instantiated generic type.
    /// </summary>
    public sealed class GenericInstanceType : GenericTypeBase, IEquatable<GenericInstanceType>
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

        /// <summary>
        /// Checks if this generic instance type equals another.
        /// </summary>
        /// <param name="other">A generic instance type.</param>
        /// <returns>
        /// <c>true</c> if the types are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(GenericInstanceType other)
        {
            return object.ReferenceEquals(this, other)
                || (object.Equals(Declaration, other.Declaration)
                    && object.Equals(ParentType, other.ParentType));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is GenericInstanceType && Equals((GenericInstanceType)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (((object)ParentType).GetHashCode() << 4) ^ ((object)Declaration).GetHashCode();
        }
    }
}