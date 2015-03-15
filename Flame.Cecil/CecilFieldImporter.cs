using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilFieldImporter : CecilTypeMemberImporterBase<IField, FieldReference>
    {
        public CecilFieldImporter(ModuleDefinition Module, IGenericParameterProvider Context)
            : base(Module, Context)
        {
        }

        protected override FieldReference ConvertDeclaration(IField Member)
        {
            return Module.Import(((ICecilField)Member).GetFieldReference(), Context);
        }

        protected override FieldReference ConvertInstanceGeneric(TypeReference DeclaringType, IField Member)
        {
            var decl = ConvertDeclaration(Member);
            return Module.Import(DeclaringType.ReferenceField(decl.Resolve()), DeclaringType);
        }
    }
}
