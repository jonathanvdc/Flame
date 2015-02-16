using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PartialMemberAccessBlock : IPartialBlock
    {
        public PartialMemberAccessBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, IPythonBlock Member)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Member = Member;
        }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            var target = PartialRedirectedBinaryOperation.Complete(Target, Arguments);
            var member = PartialRedirectedBinaryOperation.Complete(Member, Arguments);
            return new MemberAccessBlock(CodeGenerator, target, member.GetCode().ToString(), Type);
        }

        public IPythonBlock Target { get; private set; }
        public IPythonBlock Member { get; private set; }

        public IType Type { get { return Member.Type; } }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            var cb = Target.GetCode();
            cb.Append('.');
            cb.Append(Member.GetCode());
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies().MergeDependencies(Member.GetDependencies());
        }
    }
}
