using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilArrayType : CecilArrayTypeBase, IArrayType
    {
        public CecilArrayType(Mono.Cecil.ArrayType ArrayType, CecilModule Module)
            : this(Module.ConvertStrict(ArrayType.ElementType), ArrayType.Rank, Module)
        {
        }
        public CecilArrayType(ICecilType ElementType, int ArrayRank, CecilModule Module)
            : base(ElementType, Module)
        {
            this.ArrayRank = ArrayRank;
        }
        public CecilArrayType(ICecilType ElementType, int ArrayRank)
            : base(ElementType)
        {
            this.ArrayRank = ArrayRank;
        }

        public int ArrayRank { get; private set; }
        public override int GetArrayRank()
        {
            return ArrayRank;
        }

        public override IAncestryRules AncestryRules
        {
            get { return ArrayAncestryRules.Instance; }
        }
    }
}
