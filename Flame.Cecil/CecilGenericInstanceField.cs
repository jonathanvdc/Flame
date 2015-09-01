using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericInstanceField : CecilFieldBase
    {
        public CecilGenericInstanceField(ICecilType DeclaringType, ICecilField Field)
            : base(DeclaringType)
        {
            this.Field = Field;
        }

        public ICecilField Field { get; private set; }

        private FieldReference genericFieldRef;
        public override MemberReference GetMemberReference()
        {
            return GetFieldReference();
        }
        public override FieldReference GetFieldReference()
        {
            if (genericFieldRef == null)
            {
                genericFieldRef = Field.GetResolvedField().Reference(DeclaringType);
            }
            return genericFieldRef;
        }
        public override FieldDefinition GetResolvedField()
        {
            return Field.GetResolvedField();
        }

        public override IType FieldType
        {
            get
            {
                return this.DeclaringType.ResolveType(Field.FieldType);
            }
        }

        public override bool IsStatic
        {
            get
            {
                return Field.IsStatic;
            }
        }

        public override string Name
        {
            get
            {
                return Field.Name;
            }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return Field.Attributes;
        }
    }
}
