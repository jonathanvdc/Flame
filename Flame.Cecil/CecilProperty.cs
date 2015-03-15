using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilProperty : CecilPropertyBase
    {
        public CecilProperty(PropertyReference Property)
            : base(CecilTypeBase.CreateCecil(Property.DeclaringType))
        {
            this.Property = Property;
        }
        public CecilProperty(ICecilType DeclaringType, PropertyReference Property)
            : base(DeclaringType)
        {
            this.Property = Property;
        }

        public PropertyReference Property { get; private set; }

        public override PropertyReference GetPropertyReference()
        {
            return Property;
        }
        public override PropertyDefinition GetResolvedProperty()
        {
            return Property.Resolve();
        }

        public override IType PropertyType
        {
            get
            {
                return CecilTypeBase.Create(Property.PropertyType);
            }
        }
    }
}
