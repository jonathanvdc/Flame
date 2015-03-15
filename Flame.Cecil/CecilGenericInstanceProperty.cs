using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericInstanceProperty : CecilPropertyBase
    {
        public CecilGenericInstanceProperty(ICecilType DeclaringType, ICecilProperty Property)
            : base(DeclaringType)
        {
            this.Property = Property;
        }

        public ICecilProperty Property { get; private set; }

        public override PropertyReference GetPropertyReference()
        {
            return Property.GetPropertyReference();
        }

        public override string Name
        {
            get
            {
                return Property.Name;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return Property.IsStatic;
            }
        }

        private IType propType;
        public override IType PropertyType
        {
            get
            {
                if (propType == null)
                {
                    propType = DeclaringType.ResolveType(Property.PropertyType);
                }
                return propType;
            }
        }

        private IParameter[] indParams;
        public IParameter[] IndexerParameters
        {
            get
            {
                if (indParams == null)
                {
                    indParams = DeclaringType.ResolveParameters(Property.GetIndexerParameters());
                }
                return indParams;
            }
        }
        public override IParameter[] GetIndexerParameters()
        {
            return IndexerParameters;
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return Property.GetAttributes();
        }

        private IAccessor[] accs;
        public IAccessor[] Accessors
        {
            get
            {
                if (accs == null)
                {
                    accs = Property.GetAccessors().Select(item => new CecilAccessor(this, (ICecilMethod)item, item.AccessorType)).ToArray();
                }
                return accs;
            }
        }
        public override IAccessor[] GetAccessors()
        {
            return Accessors;
        }

        public override IAccessor GetAccessor
        {
            get
            {
                return this.GetGetAccessor();
            }
        }

        public override IAccessor SetAccessor
        {
            get
            {
                return this.GetSetAccessor();
            }
        }
    }
}
