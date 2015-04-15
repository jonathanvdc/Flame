using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface IAssociatedMember : ICppMember
    {
        IEnumerable<ICppMember> AssociatedMembers { get; }
    }

    public static class AssociatedMemberExtensions
    {
        public static IEnumerable<ICppMember> WithAssociatedMembers(this ICppMember Member)
        {
            if (Member is IAssociatedMember)
            {
                return new ICppMember[] { Member }.Concat(((IAssociatedMember)Member).AssociatedMembers.WithAssociatedMembers());
            }
            else
            {
                return new ICppMember[] { Member };
            }
        }

        public static IEnumerable<ICppMember> WithAssociatedMembers(this IEnumerable<ICppMember> Members)
        {
            return Members.SelectMany(WithAssociatedMembers);
        }
    }
}
