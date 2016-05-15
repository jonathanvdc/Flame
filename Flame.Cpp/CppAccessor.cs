using Flame.Build;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppAccessor : CppMethod, IAccessor
    {
        public CppAccessor(IProperty DeclaringProperty, AccessorType AccessorType, IMethodSignatureTemplate Template, ICppEnvironment Environment)
            : base((IGenericResolverType)DeclaringProperty.DeclaringType, Template, Environment)
        {
            this.AccessorType = AccessorType;
            this.DeclaringProperty = DeclaringProperty;
        }

        public IProperty DeclaringProperty { get; private set; }
        public AccessorType AccessorType { get; private set; }

        private UnqualifiedName name;
        public override UnqualifiedName Name
        {
            get
            {
                if (name == null)
                {
                    name = new SimpleName(Environment.GetAccessorNamer().Name(this));
                }
                return name;
            }
        }
    }
}
