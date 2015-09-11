using Flame.Compiler.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilAccessorBuilder : CecilMethodBuilder, IAccessor
    {
        public CecilAccessorBuilder(ICecilProperty DeclaringProperty, MethodDefinition Method, IMethodSignatureTemplate Template, AccessorType AccessorType)
            : base((ICecilType)DeclaringProperty.DeclaringType, Method, Template)
        {
            this.DeclaringProperty = DeclaringProperty;
            this.AccessorType = AccessorType;
        }

        public ICecilProperty DeclaringProperty { get; private set; }
        public AccessorType AccessorType { get; private set; }

        IProperty IAccessor.DeclaringProperty
        {
            get { return DeclaringProperty; }
        }
    }
}
