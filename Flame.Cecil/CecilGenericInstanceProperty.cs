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

        private IAccessor ConvertAccessor(IAccessor Item)
        {
            return new CecilAccessor(this, new CecilGenericInstanceMethod(DeclaringType, (ICecilMethod)Item), Item.AccessorType);
        }

        public IAccessor[] Accessors
        {
            get
            {
                return Property.GetAccessors().Select(ConvertAccessor).ToArray();
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
                return ConvertAccessor(Property.GetGetAccessor());
            }
        }

        public override IAccessor SetAccessor
        {
            get
            {
                return ConvertAccessor(Property.GetSetAccessor());
            }
        }
    }
}
