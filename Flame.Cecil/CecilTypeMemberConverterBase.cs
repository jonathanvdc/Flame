using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilTypeMemberConverterBase<TReference, TMember> : IConverter<TReference, TMember>
        where TReference : MemberReference
    {
        public CecilTypeMemberConverterBase(IConverter<TypeReference, IType> TypeConverter)
        {
            this.TypeConverter = TypeConverter;
        }
        public CecilTypeMemberConverterBase()
            : this(CecilTypeConverter.CecilPrimitiveConverter)
        {
        }

        public IConverter<TypeReference, IType> TypeConverter { get; private set; }

        protected ICecilType ConvertType(TypeReference Reference)
        {
            return (ICecilType)TypeConverter.Convert(Reference);
        }
        protected abstract TMember ConvertMemberDeclaration(ICecilType DeclaringType, TReference Reference);
        protected abstract TMember ConvertGenericInstanceMember(ICecilType DeclaringType, TReference Reference);

        public virtual TMember Convert(TReference Value)
        {
            var declRef = Value.DeclaringType;
            var convDeclRef = ConvertType(declRef);
            if (declRef.IsGenericInstance)
            {
                return ConvertGenericInstanceMember(convDeclRef, Value);
            }
            else
            {
                return ConvertMemberDeclaration(convDeclRef, Value);
            }
        }
    }
}
