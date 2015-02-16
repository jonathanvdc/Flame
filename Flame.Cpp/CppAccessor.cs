using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppAccessor : CppMethod, IAccessor
    {
        public CppAccessor(IProperty DeclaringProperty, IAccessor Template, ICppEnvironment Environment)
            : base((IGenericResolverType)DeclaringProperty.DeclaringType, Template, Environment)
        {
            this.DeclaringProperty = DeclaringProperty;
        }

        public IProperty DeclaringProperty { get; private set; }

        public AccessorType AccessorType
        {
            get { return ((IAccessor)Template).AccessorType; }
        }

        public override string Name
        {
            get
            {
                if (AccessorType.Equals(AccessorType.GetAccessor))
                {
                    return "get" + DeclaringProperty.Name;
                }
                else if (AccessorType.Equals(AccessorType.SetAccessor))
                {
                    return "set" + DeclaringProperty.Name;
                }
                else
                {
                    return base.Name;
                }
            }
        }
    }
}
