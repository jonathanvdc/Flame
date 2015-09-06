using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ToAddressBlock : ICppBlock, IPointerBlock
    {
        public ToAddressBlock(ICppBlock Target)
        {
            this.Target = Target;
        }

        public ICppBlock Target { get; private set; }

        public IType Type
        {
            get { return Target.Type.AsContainerType().ElementType.MakePointerType(PointerKind.TransientPointer); }
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
            cb.Append(Target.GetCode());
            cb.Append(".get()");
            return cb;
        }

        public ICppBlock StaticDereference()
        {
            return Target;
        }
    }
}
