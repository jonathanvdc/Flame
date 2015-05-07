using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public abstract class RecompiledTypeMemberTemplate : RecompiledMemberTemplate, ITypeMember
    {
        public RecompiledTypeMemberTemplate(AssemblyRecompiler Recompiler)
            : base(Recompiler)
        { }

        public abstract ITypeMember GetSourceTypeMember();
        public override IMember GetSourceMember()
        {
            return GetSourceTypeMember();
        }

        public IType DeclaringType
        {
            get
            {
                var declType = GetSourceTypeMember().DeclaringType;
                return declType == null ? null : Recompiler.GetType(declType);
            }
        }

        public bool IsStatic
        {
            get { return GetSourceTypeMember().IsStatic; }
        }
    }
}
