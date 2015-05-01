using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public sealed class CecilMethodConverter : CecilTypeMemberConverterBase<MethodReference, ICecilMethod>
    {
        public CecilMethodConverter(CecilModule Module, IDictionary<MethodReference, ICecilMethod> Cache)
            : base(Module, Cache)
        {
        }

        protected override ICecilMethod ConvertMemberDeclaration(ICecilType DeclaringType, MethodReference Reference)
        {
            return new CecilMethod(DeclaringType, Reference);
        }

        protected override ICecilMethod ConvertGenericInstanceMember(ICecilType DeclaringType, MethodReference Value)
        {
            var elemMethod = ConvertMemberDeclaration(DeclaringType, Value);
            return new CecilGenericInstanceMethod(DeclaringType, elemMethod);
        }

        private ICecilMethod ConvertGenericMethodInstance(GenericInstanceMethod Instance)
        {
            var elemMethod = Convert(Instance.ElementMethod);
            var genArgs = Instance.GenericArguments.Select(Module.Convert).ToArray();
            return (ICecilMethod)elemMethod.MakeGenericMethod(genArgs);
        }

        public override ICecilMethod Convert(MethodReference Value)
        {
            if (Value.IsGenericInstance)
            {
                return ConvertGenericMethodInstance((GenericInstanceMethod)Value);
            }
            else
            {
                return base.Convert(Value);
            }
        }
    }
}
