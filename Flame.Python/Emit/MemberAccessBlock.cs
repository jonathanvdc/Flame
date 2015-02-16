using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.Python.Emit
{
    public class MemberAccessBlock : IPythonBlock
    {
        public MemberAccessBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, string Member, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Member = Member;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IPythonBlock Target { get; private set; }
        public string Member { get; private set; }
        public IType Type { get; private set; }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            if (Target is BinaryOperation)
            {
                cb.Append(BinaryOperation.GetEnclosedCode(Target));
            }
            else
            {
                cb.Append(Target.GetCode());
            }
            cb.Append(".");
            cb.Append(Member);
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies();
        }
    }
}
