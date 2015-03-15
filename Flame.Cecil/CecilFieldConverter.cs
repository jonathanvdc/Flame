using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilFieldConverter : CecilTypeMemberConverterBase<FieldReference, ICecilField>
    {
        protected override ICecilField ConvertMemberDeclaration(ICecilType DeclaringType, FieldReference Reference)
        {
            return new CecilField(DeclaringType, Reference);
        }

        protected override ICecilField ConvertGenericInstanceMember(ICecilType DeclaringType, FieldReference Reference)
        {
            var decl = ConvertMemberDeclaration(DeclaringType, Reference);
            return new CecilGenericInstanceField(DeclaringType, decl);
        }
    }
}
