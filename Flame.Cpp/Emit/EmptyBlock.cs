using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class EmptyBlock : ICppBlock
    {
        public EmptyBlock(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
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
