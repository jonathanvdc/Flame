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
        public DefaultCppTypeConverter(ICppEnvironment Environment)
        {
            this.Environment = Environment;
        }

        public ICppEnvironment Environment { get; private set; }

        protected override IType MakePointerType(IType Type, PointerKind Kind)
        {
            return Type.MakePointerType(Kind);
        }

        protected override IType MakeReferenceType(IType Type)
        {
            return MakePointerType(Type, Type.GetReferencePointerKind());
        }

        protected override IType MakeArrayType(IType Type, int ArrayRank)
        {
            if (ArrayRank == 1)
            {
                return Environment.GetStdxNamespace().ArraySlice.MakeGenericType(new IType[] { Type });
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override IType MakeVectorType(IType Type, IReadOnlyList<int> Dimensions)
        {
            throw new NotImplementedException();
        }
    }
}
