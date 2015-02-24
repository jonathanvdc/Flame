using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialEmptyBlock : IPartialBlock
    {
        public PartialEmptyBlock(ICodeGenerator CodeGenerator, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get; private set; }

        public ICppBlock Complete(PartialArguments Arguments)
        {
            return new PartialEmptyBlock(CodeGenerator, Type);
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Enumerable.Empty<IHeaderDependency>(); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Enumerable.Empty<CppLocal>(); }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder();
        }
    }
}
