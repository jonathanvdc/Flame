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
        public CecilTypeMemberConverterBase(CecilModule Module, IDictionary<TReference, TMember> Cache)
        {
            this.Module = Module;
            this.convertedValues = Cache;
        }

        private IDictionary<TReference, TMember> convertedValues;

        public CecilModule Module { get; private set; }

        protected IType ConvertType(TypeReference Reference)
        {
            return Module.ConvertStrict(Reference);
        }
        protected abstract TMember ConvertMemberDeclaration(ICecilType DeclaringType, TReference Reference);
        protected abstract TMember ConvertGenericInstanceMember(GenericTypeBase DeclaringType, TReference Reference);

        public virtual TMember Convert(TReference Value)
        {
            if (convertedValues.ContainsKey(Value))
            {
                return convertedValues[Value];
            }

            var declRef = Value.DeclaringType;
            var convDeclRef = ConvertType(declRef);
            if (declRef.IsGenericInstance)
            {
                return ConvertGenericInstanceMember((GenericTypeBase)convDeclRef, Value);
            }
            else
            {
                return ConvertMemberDeclaration((ICecilType)convDeclRef, Value);
            }
        }
    }
}
