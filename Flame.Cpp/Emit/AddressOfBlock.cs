using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class AddressOfBlock : ICppBlock, IPointerBlock
    {
        public AddressOfBlock(ICppBlock Target)
        {
            this.Target = Target;
        }

        public ICppBlock Target { get; private set; }

        public IType Type
        {
            get { return Target.Type.MakePointerType(PointerKind.TransientPointer); }
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
            CodeBuilder cb = new CodeBuilder();
            cb.Append('&');
            cb.Append(Target.GetCode());
            return cb;
        }

        public ICppBlock StaticDereference()
        {
            return Target;
        }
    }
}
