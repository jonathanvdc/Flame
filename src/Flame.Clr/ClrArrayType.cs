using System;
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
        /// <param name="createBaseTypes">
        /// Creates the array type's base types based on a
        /// generic parameter that represents the array's element
        /// type.
        /// </param>
        internal ClrArrayType(int rank, Func<IGenericParameter, IReadOnlyList<IType>> createBaseTypes)
        {
            this.Rank = rank;
            var genericParam = new DescribedGenericParameter(this, "T");
            this.BaseTypes = createBaseTypes(genericParam);
            this.genericParamList = new IGenericParameter[] { genericParam };
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

        /// <summary>
        /// Gets a CLR array type's element type, provided that a CLR array
        /// type was indeed provided.
        /// </summary>
        /// <param name="arrayType">
        /// The type to inspect, which might be a CLR array type.
        /// </param>
        /// <param name="elementType">
        /// An output value that is set to the type of element stored in the array,
        /// provided that <paramref name="arrayType"/> is a CLR array type.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="arrayType"/> is a CLR array type;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool TryGetArrayElementType(IType arrayType, out IType elementType)
        {
            var genericSpecialization = arrayType as DirectTypeSpecialization;
            if (genericSpecialization == null || !(genericSpecialization.Declaration is ClrArrayType))
            {
                elementType = null;
                return false;
            }
            else
            {
                elementType = genericSpecialization.GenericArguments[0];
                return true;
            }
        }

        /// <summary>
        /// Gets a CLR array type's rank, provided that a CLR array
        /// type was indeed provided.
        /// </summary>
        /// <param name="arrayType">
        /// The type to inspect, which might be a CLR array type.
        /// </param>
        /// <param name="rank">
        /// An output value that is set to the array type's rank,
        /// provided that <paramref name="arrayType"/> is a CLR array type.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="arrayType"/> is a CLR array type;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool TryGetArrayRank(IType arrayType, out int rank)
        {
            var genericSpecialization = arrayType as DirectTypeSpecialization;
            if (genericSpecialization == null || !(genericSpecialization.Declaration is ClrArrayType))
            {
                rank = 0;
                return false;
            }
            else
            {
                rank = ((ClrArrayType)genericSpecialization.Declaration).Rank;
                return true;
            }
        }

        /// <summary>
        /// Determines if a type is a CLR array type.
        /// </summary>
        /// <param name="type">A type to examine.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="type"/> is an array type; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsArrayType(IType type)
        {
            int rank;
            return TryGetArrayRank(type, out rank);
        }
    }

    /// <summary>
    /// A CLR array type comparer based on rank.
    /// </summary>
    internal sealed class RankClrArrayTypeComparer : IEqualityComparer<ClrArrayType>
    {
        public bool Equals(ClrArrayType x, ClrArrayType y)
        {
            return x.Rank == y.Rank;
        }

        public int GetHashCode(ClrArrayType obj)
        {
            return obj.Rank;
        }
    }
}
