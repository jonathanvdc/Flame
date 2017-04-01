using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    /// <summary>
    /// A type converter that detects foreign (non-C++) types.
    /// </summary>
    public sealed class ForeignTypePredicate : TypeConverterBase<bool>
    {
        private ForeignTypePredicate()
        { }

        private static ForeignTypePredicate inst;
        public static ForeignTypePredicate Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new ForeignTypePredicate();
                }
                return inst;
            }
        }

        protected override bool ConvertTypeDefault(IType Type)
        {
            return !(Type is ICppMember);
        }

        protected override bool MakeArrayType(bool ElementType, int ArrayRank)
        {
            return ElementType;
        }

        protected override bool MakeGenericType(bool GenericDeclaration, IEnumerable<bool> TypeArguments)
        {
            return GenericDeclaration;
        }

        protected override bool MakeGenericInstanceType(bool GenericDeclaration, bool GenericDeclaringTypeInstance)
        {
            return GenericDeclaration;
        }

        protected override bool MakePointerType(bool ElementType, PointerKind Kind)
        {
            return ElementType;
        }

        protected override bool MakeVectorType(bool ElementType, IReadOnlyList<int> Dimensions)
        {
            return ElementType;
        }
    }
}
