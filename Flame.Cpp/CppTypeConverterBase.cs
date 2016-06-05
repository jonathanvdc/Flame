using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public abstract class CppTypeConverterBase : TypeTransformerBase, ICppTypeConverter
    {
        public CppTypeConverterBase()
        {

        }

        protected abstract override IType MakePointerType(IType Type, PointerKind Kind);
        protected abstract IType MakeReferenceType(IType Type);
        protected abstract override IType MakeArrayType(IType Type, int ArrayRank);
        protected abstract override IType MakeVectorType(IType Type, IReadOnlyList<int> Dimensions);

        protected override IType ConvertPointerType(PointerType Type)
        {
            return MakePointerType(ConvertWithValueSemantics(Type.ElementType), Type.PointerKind);
        }

        protected virtual IType ConvertValueGenericInstance(IType Type)
        {
            return MakeGenericType(ConvertWithValueSemantics(Type.GetGenericDeclaration()), Convert(Type.GetGenericArguments()));
        }

        protected override IType ConvertGenericInstance(IType Type)
        {
            var genDecl = Type.GetGenericDeclaration();
            if (genDecl.GetIsReferenceType())
            {
                return MakeReferenceType(ConvertValueGenericInstance(Type));
            }
            else
            {
                return base.ConvertGenericInstance(Type);
            }
        }

        protected override IType ConvertReferenceType(IType Type)
        {
            if (Type.Equals(PrimitiveTypes.Null))
            {
                return Type;
            }
            return MakeReferenceType(Type);
        }

        protected override IType ConvertPrimitiveType(IType Type)
        {
            return Type;
        }

        public virtual IType ConvertWithValueSemantics(IType Type)
        {
            if (Type.GetIsGenericInstance())
            {
                return ConvertValueGenericInstance(Type);
            }
            else if (Type.GetIsReferenceType() && !Type.GetIsContainerType() && !Type.GetIsDelegate())
            {
                return ConvertValueType(Type);
            }
            else
            {
                return Convert(Type);
            }
        }

        protected override IType ConvertTypeDefault(IType Type)
        {
            return Type;
        }

        protected override IType MakeGenericType(IType GenericDeclaration, IEnumerable<IType> TypeArguments)
        {
            return GenericDeclaration.MakeGenericType(TypeArguments);
        }
    }
}
