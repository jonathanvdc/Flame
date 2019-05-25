using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A special type that signals that an error occurred and that the
    /// corresponding error has been reported already.
    ///
    /// Further errors that arise because an error type is encountered
    /// should be suppressed. For example, it is wrong to report an error
    /// when a member of an error type is accessed. Reporting the error
    /// would confuse the user---the true error here is that the type of
    /// a value cannot be recovered, not that the error type does not have
    /// any members.
    /// </summary>
    public sealed class ErrorType : IType
    {
        private ErrorType()
        { }

        /// <summary>
        /// An error type.
        /// </summary>
        public static readonly ErrorType Instance = new ErrorType();

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
        public IReadOnlyList<IType> NestedTypes => EmptyArray<IType>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => EmptyArray<IGenericParameter>.Value;

        /// <inheritdoc/>
        public UnqualifiedName Name => new SimpleName("<error type>");

        /// <inheritdoc/>
        public QualifiedName FullName => Name.Qualify();

        /// <inheritdoc/>
        public AttributeMap Attributes => AttributeMap.Empty;
    }
}