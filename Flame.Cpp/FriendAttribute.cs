using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Primitives;
using Flame.Build;

namespace Flame.Cpp
{
    public class FriendAttribute : IAttribute
    {
        static FriendAttribute()
        {
            attrType = new PrimitiveType<IAttribute>("FriendAttribute", 0, null);
        }

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
