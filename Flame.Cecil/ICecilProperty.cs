using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public interface ICecilProperty : IProperty
    {
        PropertyReference GetPropertyReference();
    }
    public interface ICecilPropertyBuilder : ICecilProperty, IPropertyBuilder
    {
        void AddAccessor(MethodDefinition Method, AccessorType Kind);
    }
}
