using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A generic parameter that can be constructed piece by piece in
    /// an imperative fashion.
    /// </summary>
    public sealed class DescribedGenericParameter : DescribedGenericMember, IGenericParameter
    {
        /// <summary>
        /// Creates a generic parameter from a declaring member
        /// and a name.
        /// </summary>
        /// <param name="parentMember">
        /// The member that declares the generic parameter.
        /// </param>
        /// <param name="name">
        /// The generic parameter's name.
        /// </param>
        public DescribedGenericParameter(
            IGenericMember parentMember,
            SimpleName name)
            : base(name.Qualify(parentMember.FullName))
        {
            this.ParentMember = parentMember;
        }

        /// <summary>
        /// Creates a generic parameter from a declaring member
        /// and a name.
        /// </summary>
        /// <param name="parentMember">
        /// The member that declares the generic parameter.
        /// </param>
        /// <param name="name">
        /// The generic parameter's name.
        /// </param>
        public DescribedGenericParameter(
            IGenericMember parentMember,
            string name)
            : this(parentMember, new SimpleName(name))
        { }

        /// <inheritdoc/>
        public IGenericMember ParentMember { get; private set; }

        /// <inheritdoc/>
        public TypeParent Parent
        {
            get
            {
                if (ParentMember is IMethod)
                {
                    return new TypeParent((IMethod)ParentMember);
                }
                else
                {
                    return new TypeParent((IType)ParentMember);
                }
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes { get; set; }

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