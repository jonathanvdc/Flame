using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// An IL array type of a particular rank.
    /// </summary>
    public sealed class ClrArrayType : IType
    {
        /// <summary>
        /// Creates a new array type of a particular rank and with
        /// a particular list of base types.
        /// </summary>
        /// <param name="rank">The array type's rank.</param>
        /// <param name="baseTypes">The array type's base types.</param>
        internal ClrArrayType(int rank, IReadOnlyList<IType> baseTypes)
        {
            this.Rank = rank;
            this.BaseTypes = baseTypes;
            this.genericParamList = new IGenericParameter[]
            {
                new DescribedGenericParameter(this, "T")
            };
            this.FullName = new SimpleName("array!" + Rank, 1).Qualify();
            this.Attributes = new AttributeMap(new[] { FlagAttribute.ReferenceType });
        }

        /// <summary>
        /// Gets the rank of this array type.
        /// </summary>
        /// <returns>The array type's rank.</returns>
        public int Rank { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes { get; private set; }

        private IReadOnlyList<IGenericParameter> genericParamList;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => genericParamList;

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes { get; private set; }

        /// <inheritdoc/>
        public TypeParent Parent => TypeParent.Nothing;

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields => EmptyArray<IField>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods => EmptyArray<IMethod>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties => EmptyArray<IProperty>.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes => EmptyArray<IType>.Value;
    }
}
