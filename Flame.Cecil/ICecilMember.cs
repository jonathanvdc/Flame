using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public interface ICecilComponent
    {
        CecilModule Module { get; }
    }

    public interface ICecilMember : ICecilComponent, IMember
    {
        MemberReference GetMemberReference();
    }
    public interface ICecilGenericMember : ICecilMember, IGenericMember
    {
    }
}
