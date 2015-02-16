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
            : base(DeclaringType.GetAncestryGraph())
        {
            this.DeclaringType = DeclaringType;
        }

        public ICecilType DeclaringType { get; private set; }

        IType ITypeMember.DeclaringType
        {
            get { return DeclaringType; }
        }

        public abstract bool IsStatic { get; }
        protected abstract IType ResolveLocalTypeParameter(IGenericParameter TypeParameter);

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

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            var localParam = ResolveLocalTypeParameter(TypeParameter);
            if (localParam == null)
            {
                return DeclaringType.ResolveTypeParameter(TypeParameter);
            }
            else
            {
                return localParam;
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
