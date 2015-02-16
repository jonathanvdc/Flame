using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledVariableMember : RecompiledMemberTemplate, IVariableMember
    {
        public RecompiledVariableMember(AssemblyRecompiler Recompiler, IVariableMember SourceMember)
            : base(Recompiler)
        {
            this.SourceMember = SourceMember;
        }

        public IVariableMember SourceMember { get; private set; }
        public override IMember GetSourceMember()
        {
            return SourceMember;
        }

        public IType VariableType
        {
            get { return Recompiler.GetType(SourceMember.VariableType); }
        }
    }
}
