using System;
using System.Collections.Generic;
using Flame.Build;

namespace Flame.Cecil
{
    /// <summary>
    /// Represents a CLR array, i.e., CLR environment's equivalent type of a Flame array. 
    /// </summary>
    public sealed class CecilArrayType : ContainerTypeBase
    {
        public CecilArrayType(IType ElementType, int ArrayRank, CecilModule Module)
            : base(ElementType)
        {
            this.ArrayRank = ArrayRank;
            this.Module = Module;
            var typeSystem = Module.TypeSystem;
            var listType = typeSystem.IListType.MakeGenericType(new IType[] { ElementType });
            if (typeSystem.IReadOnlyListType == null)
            {
                this.baseTypes = new IType[]
                {
                    typeSystem.ArrayType,
                    listType
                };
            }
            else
            {
                this.baseTypes = new IType[]
                {
                    typeSystem.ArrayType,
                    listType,
                    typeSystem.IReadOnlyListType.MakeGenericType(new IType[] { ElementType })
                };
            }
        }

        /// <summary>
        /// Gets the number of dimensions in this array type.
        /// </summary>
        public int ArrayRank { get; private set; }

        /// <summary>
        /// Gets this array type's module.
        /// </summary>
        /// <returns>The array type's module.</returns>
        public CecilModule Module { get; private set; }

        private IEnumerable<IType> baseTypes;

        public override IAncestryRules AncestryRules => ArrayAncestryRules.Instance;

        protected override UnqualifiedName GetName(QualifiedName ElementName)
        {
            return new ArrayName(ElementName, ArrayRank);
        }

        public override IEnumerable<IType> BaseTypes
        {
            get { return baseTypes; }
        }

        public override bool Equals(object obj)
        {
            if (obj is CecilArrayType)
            {
                var arrType = (CecilArrayType)obj;
                return ArrayRank == arrType.ArrayRank
                    && ElementType.Equals(arrType.ElementType);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            // Compute the one's complement of the array hash code to keep us
            // from clashing with ArrayType's hash code.
            return ~(ElementType.GetHashCode() + ArrayRank);
        }
    }
}