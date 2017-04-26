using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Build;

namespace Flame.Cecil
{
    /// <summary>
    /// Represents a CLR array, i.e., the CLR environment's equivalent of a Flame array.
    /// </summary>
    public sealed class CecilArrayType : IType
    {
        public CecilArrayType(int ArrayRank, CecilModule Module)
        {
            var elemType = new DescribedGenericParameter("T", this);
            this.genericParams = new IGenericParameter[] { elemType };
            this.ArrayRank = ArrayRank;
            this.Module = Module;
            var typeSystem = Module.TypeSystem;
            var listType = typeSystem.IListType.MakeGenericType(new IType[] { elemType });
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
                    typeSystem.IReadOnlyListType.MakeGenericType(new IType[] { elemType })
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
        private IEnumerable<IGenericParameter> genericParams;

        public IAncestryRules AncestryRules => ArrayAncestryRules.Instance;

        public UnqualifiedName Name
        {
            get { return new SimpleName("ClrArrayRank" + ArrayRank, 1); }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return baseTypes; }
        }

        public INamespace DeclaringNamespace => null;

        public IEnumerable<IMethod> Methods => Enumerable.Empty<IMethod>();

        public IEnumerable<IProperty> Properties => Enumerable.Empty<IProperty>();

        public IEnumerable<IField> Fields => Enumerable.Empty<IField>();

        public IEnumerable<IGenericParameter> GenericParameters => genericParams;

        public AttributeMap Attributes => AttributeMap.Empty;

        public QualifiedName FullName => new QualifiedName(Name);

        public override bool Equals(object obj)
        {
            if (obj is CecilArrayType)
            {
                var arrType = (CecilArrayType)obj;
                return ArrayRank == arrType.ArrayRank;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return ArrayRank << 4;
        }

        public IBoundObject GetDefaultValue()
        {
            return null;
        }
    }
}