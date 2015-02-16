using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public interface ICecilMethod : ICecilTypeMember, ICecilGenericMember, IMethod
    {
        MethodReference GetMethodReference();
        bool IsComplete { get; }
    }
    public interface ICecilMethodBuilder : ICecilMethod, IMethodBuilder
    {

    }
}
