using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface IAccessorNamer
    {
        string Name(IAccessor Accessor);
    }

    public sealed class OverloadedAccessorNamer : IAccessorNamer
    {
        private OverloadedAccessorNamer() { }

        static OverloadedAccessorNamer()
        {
            inst = new OverloadedAccessorNamer();
        }

        private static OverloadedAccessorNamer inst;
        public static OverloadedAccessorNamer Instance
        {
            get
            {
                return inst;
            }
        }

        public string Name(IAccessor Accessor)
        {
            return Accessor.DeclaringProperty.Name.ToString();
        }
    }

    public sealed class LowerCaseAccessorNamer : IAccessorNamer
    {
        private LowerCaseAccessorNamer() { }

        static LowerCaseAccessorNamer()
        {
            inst = new LowerCaseAccessorNamer();
        }

        private static LowerCaseAccessorNamer inst;
        public static LowerCaseAccessorNamer Instance
        {
            get
            {
                return inst;
            }
        }

        public string Name(IAccessor Accessor)
        {
            var type = Accessor.AccessorType;
            string propName = Accessor.DeclaringProperty.Name.ToString();
            if (type.Equals(AccessorType.GetAccessor))
                return "get" + propName;
            else if (type.Equals(AccessorType.SetAccessor))
                return "set" + propName;
            else if (type.Equals(AccessorType.AddAccessor))
                return "add" + propName;
            else if (type.Equals(AccessorType.RemoveAccessor))
                return "remove" + propName;
            else
                return type.ToString().ToLower() + propName;
        }
    }

    public sealed class UpperCamelCaseAccessorNamer : IAccessorNamer
    {
        private UpperCamelCaseAccessorNamer() { }

        static UpperCamelCaseAccessorNamer()
        {
            inst = new UpperCamelCaseAccessorNamer();
        }

        private static UpperCamelCaseAccessorNamer inst;
        public static UpperCamelCaseAccessorNamer Instance
        {
            get
            {
                return inst;
            }
        }

        public string Name(IAccessor Accessor)
        {
            var type = Accessor.AccessorType;
            string propName = Accessor.DeclaringProperty.Name.ToString();
            if (type.Equals(AccessorType.GetAccessor))
                return "Get" + propName;
            else if (type.Equals(AccessorType.SetAccessor))
                return "Set" + propName;
            else if (type.Equals(AccessorType.AddAccessor))
                return "Add" + propName;
            else if (type.Equals(AccessorType.RemoveAccessor))
                return "Remove" + propName;
            else
            {
                string name = type.ToString();
                return name.Substring(0, 1).ToUpper() + name.Substring(1).ToLower() + propName;
            }
        }
    }
}
