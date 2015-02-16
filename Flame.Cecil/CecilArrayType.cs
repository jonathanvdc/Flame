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
        public CecilArrayType(ArrayType ArrayType)
            : this(CecilTypeBase.CreateCecil(ArrayType.ElementType), ArrayType.Rank)
        {
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
    }
}
