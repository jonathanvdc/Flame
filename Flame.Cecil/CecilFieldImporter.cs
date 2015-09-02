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
        public CecilFieldImporter(CecilModule Module)
            : base(Module)
        {
        }
        public CecilFieldImporter(CecilModule Module, IGenericParameterProvider Context)
            : base(Module, Context)
        {
        }

        public static FieldReference Import(CecilModule Module, IField Field)
        {
            return new CecilFieldImporter(Module).Convert(Field);
        }
        public static FieldReference Import(CecilModule Module, IGenericParameterProvider Context, IField Field)
        {
            return new CecilFieldImporter(Module, Context).Convert(Field);
        }

        protected override FieldReference ConvertDeclaration(IField Member)
        {
            return Module.Module.Import(((ICecilField)Member).GetFieldReference(), Context);
        }

        protected override FieldReference ConvertInstanceGeneric(TypeReference DeclaringType, IField Member)
        {
            var decl = ConvertDeclaration(Member.GetRecursiveGenericDeclaration());
            return Module.Module.Import(DeclaringType.ReferenceField(decl.Resolve()), DeclaringType);
        }
    }
}
