using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilTypeMemberBase : CecilMember, ICecilTypeMember
    {
        public CecilTypeMemberBase(ICecilType DeclaringType)
            : base(DeclaringType.Module)
        {
            this.DeclaringType = DeclaringType;
        }

        public ICecilType DeclaringType { get; private set; }

        IType ITypeMember.DeclaringType
        {
            get { return DeclaringType; }
        }

        public abstract bool IsStatic { get; }

        public override string Name
        {
            get { return GetMemberReference().Name; }
        }

        public override string FullName
        {
            get
            {
                return MemberExtensions.CombineNames(DeclaringType.FullName, Name);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ICecilTypeMember)
            {
                return GetMemberReference().Equals(((ICecilTypeMember)obj).GetMemberReference());
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return GetMemberReference().GetHashCode();
        }
    }
}
