using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public sealed class CecilMethodConverter : CecilTypeMemberConverterBase<MethodReference, IMethod>
    {
        public CecilMethodConverter(CecilModule Module, IDictionary<MethodReference, IMethod> Cache)
            : base(Module, Cache)
        {
        }

        protected override IMethod ConvertMemberDeclaration(ICecilType DeclaringType, MethodReference Reference)
        {
            return new CecilMethod(DeclaringType, Reference);
        }

        protected override IMethod ConvertGenericInstanceMember(GenericTypeBase DeclaringType, MethodReference Value)
        {
            var genericDeclType = (ICecilType)DeclaringType.GetRecursiveGenericDeclaration();
            var inner = ConvertMemberDeclaration(genericDeclType, Value.Resolve());
            return new Flame.GenericInstanceMethod(inner, DeclaringType.Resolver, DeclaringType);
        }

        private ICecilMethod ConvertGenericMethodInstance(Mono.Cecil.GenericInstanceMethod Instance)
        {
            var elemMethod = Convert(Instance.ElementMethod);
            var genArgs = Instance.GenericArguments.Select(Module.Convert).ToArray();
            return (ICecilMethod)elemMethod.MakeGenericMethod(genArgs);
        }

        public override IMethod Convert(MethodReference Value)
        {
            if (Value.IsGenericInstance)
            {
                return ConvertGenericMethodInstance((Mono.Cecil.GenericInstanceMethod)Value);
            }
            else
            {
                return base.Convert(Value);
            }
        }
    }
}
