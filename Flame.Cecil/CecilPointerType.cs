using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilPointerType : CecilContainerTypeBase, IPointerType
    {
        public CecilPointerType(Mono.Cecil.PointerType PointerType, CecilModule Module)
            : this(Module.ConvertStrict(PointerType.ElementType), PointerKind.TransientPointer, Module)
        {
        }
        public CecilPointerType(ByReferenceType ByReferenceType, CecilModule Module)
            : this(Module.ConvertStrict(ByReferenceType.ElementType), PointerKind.ReferencePointer, Module)
        {
        }
        public CecilPointerType(ICecilType ElementType, PointerKind PointerKind, CecilModule Module)
            : base(ElementType, Module)
        {
            this.PointerKind = PointerKind;
        }
        public CecilPointerType(ICecilType ElementType, PointerKind PointerKind)
            : base(ElementType)
        {
            this.PointerKind = PointerKind;
        }

        public static TypeReference CreatePointerReference(TypeReference ElementType, PointerKind PointerKind)
        {
            if (PointerKind.Equals(PointerKind.TransientPointer))
            {
                return new Mono.Cecil.PointerType(ElementType);
            }
            else
            {
                return new ByReferenceType(ElementType);
            }
        }

        public override TypeReference GetTypeReference()
        {
            return CreatePointerReference(ElementType.GetTypeReference(), PointerKind);
        }

        public PointerKind PointerKind { get; private set; }

        public override IEnumerable<IType> BaseTypes
        {
            get { return new IType[0]; }
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            return new IAttribute[] 
            { 
                new AccessAttribute(AccessModifier.Public),
                PrimitiveAttributes.Instance.ValueTypeAttribute
            };
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return new CustomAttribute[0];
        }

        public override IBoundObject GetDefaultValue()
        {
            return null;
        }

        protected override IList<MethodDefinition> GetCecilMethods()
        {
            return new MethodDefinition[0];
        }

        protected override IList<PropertyDefinition> GetCecilProperties()
        {
            return new PropertyDefinition[0];
        }

        protected override IList<FieldDefinition> GetCecilFields()
        {
            return new FieldDefinition[0];
        }

        protected override IList<EventDefinition> GetCecilEvents()
        {
            return new EventDefinition[0];
        }

        public override IAncestryRules AncestryRules
        {
            get { return PointerAncestryRules.Instance; }
        }

        protected override bool ContainerEquals(IContainerType other)
        {
            return other.get_IsPointer() && (other.AsPointerType().PointerKind.Equals(PointerKind));
        }

        protected override string GetName()
        {
            return ElementType.Name + PointerKind.Extension;
        }

        protected override string GetFullName()
        {
            return ElementType.FullName + PointerKind.Extension;
        }
    }
}
