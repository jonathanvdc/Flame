using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class EmptyBlock : IPythonBlock
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

        public CodeBuilder GetCode()
        {
            return new CodeBuilder();
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Enumerable.Empty<ModuleDependency>();
        }
    }
}
