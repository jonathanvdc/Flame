using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialElementBlock : IPartialBlock
    {
        public PartialElementBlock(ICppBlock Target, IType Type)
        {
            this.Target = Target;
            this.Type = Type;
        }

        public ICppBlock Target { get; private set; }
        public IType Type { get; private set; }

        public ICppBlock Complete(PartialArguments Arguments)
        {
            return new ElementBlock(Target, Arguments.Single(), Type);
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder(Target.GetCode());
            cb.Append("[]");
            return cb;
        }
    }
}
