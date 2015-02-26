using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class HeaderDependencyAttribute : IAttribute
    {
        static HeaderDependencyAttribute()
		{
			attrType = new Flame.Primitives.PrimitiveType<IAttribute>("HeaderDependency", 0, null);
		}

        public HeaderDependencyAttribute(IHeaderDependency Dependency)
        {
            this.Dependency = Dependency;
        }

        public IHeaderDependency Dependency { get; private set; }

        private static IType attrType;
        public IType AttributeType
        {
            get { return attrType; }
        }

        public IBoundObject Value
        {
            get { return new BoundPrimitive<IAttribute>(AttributeType, this); }
        }
    }
}
