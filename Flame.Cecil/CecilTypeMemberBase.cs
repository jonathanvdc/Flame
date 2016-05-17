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

        public override UnqualifiedName Name
        {
            get { return new SimpleName(GetMemberReference().Name); }
        }

        public override QualifiedName FullName
        {
            get
            {
                return Name.Qualify(DeclaringType.FullName);
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
