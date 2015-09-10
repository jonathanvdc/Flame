using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public abstract class RecompiledTypeMemberTemplate<T> : RecompiledMemberTemplate<T>, ITypeMemberSignatureTemplate<T>
        where T : ITypeMember
    {
        public RecompiledTypeMemberTemplate(AssemblyRecompiler Recompiler)
            : base(Recompiler)
        { }

        public bool IsStatic
        {
            get { return GetSourceMember().IsStatic; }
        }
    }
}
