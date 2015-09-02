using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilVectorType : CecilArrayTypeBase, IVectorType
    {
        public CecilVectorType(ICecilType ElementType, IReadOnlyList<int> Dimensions)
            : base(ElementType)
        {
            this.Dimensions = Dimensions;
        }

        public static TypeReference CreateVectorReference(TypeReference ElementType, IReadOnlyList<int> Dimensions)
        {
            return CreateArrayReference(ElementType, Dimensions.Count);
        }

        public IReadOnlyList<int> Dimensions { get; private set; }
        public override int GetArrayRank()
        {
            return Dimensions.Count;
        }

        public override IAncestryRules AncestryRules
        {
            get { return VectorAncestryRules.Instance; }
        }

        private string AppendVectorSuffix(string Name)
        {
            StringBuilder sb = new StringBuilder(Name);
            sb.Append('[');
            sb.Append(Dimensions[0]);
            for (int i = 1; i < GetArrayRank(); i++)
            {
                sb.Append(", ");
                sb.Append(Dimensions[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        protected override string GetName()
        {
            return AppendVectorSuffix(ElementType.Name);
        }

        protected override string GetFullName()
        {
            return AppendVectorSuffix(ElementType.FullName);
        }
    }
}
