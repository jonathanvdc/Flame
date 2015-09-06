using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilFieldConverter : CecilTypeMemberConverterBase<FieldReference, IField>
    {
        public CecilFieldConverter(CecilModule Module, IDictionary<FieldReference, IField> Cache)
            : base(Module, Cache)
        {

        }

        protected override IField ConvertMemberDeclaration(ICecilType DeclaringType, FieldReference Reference)
        {
            return new CecilField(DeclaringType, Reference);
        }

        protected override IField ConvertGenericInstanceMember(GenericTypeBase DeclaringType, FieldReference Reference)
        {
            var decl = ConvertMemberDeclaration((ICecilType)DeclaringType.GetRecursiveGenericDeclaration(), Reference);
            return new Flame.GenericInstanceField(decl, DeclaringType.Resolver, DeclaringType);
        }
    }
}
