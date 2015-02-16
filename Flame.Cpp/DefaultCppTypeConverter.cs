using Flame.Cpp.Plugs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class DefaultCppTypeConverter : CppTypeConverterBase
    {
        public DefaultCppTypeConverter()
        {

        }

        protected override IType MakePointerType(IType Type, PointerKind Kind)
        {
            return Type.MakePointerType(Kind);
        }

        protected override IType MakeReferenceType(IType Type)
        {
            return MakePointerType(Type, PointerKind.ReferencePointer);
        }

        protected override IType MakeArrayType(IType Type, int ArrayRank)
        {
            if (ArrayRank == 1)
            {
                return StdxArraySlice.Instance.MakeGenericType(new IType[] { Type });
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override IType MakeVectorType(IType Type, int[] Dimensions)
        {
            throw new NotImplementedException();
        }
    }
}
