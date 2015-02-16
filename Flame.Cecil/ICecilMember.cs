using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public interface ICecilMember : IMember
    {
        MemberReference GetMemberReference();
    }
    public interface ICecilGenericMember : ICecilMember
    {
        IEnumerable<IType> GetCecilGenericArguments();
        IEnumerable<IGenericParameter> GetCecilGenericParameters();
    }
}
