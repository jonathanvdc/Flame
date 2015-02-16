using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;

namespace Flame.Python.Emit
{
    public class ImplicitlyConvertedBlock : IPythonBlock
    {
        public ImplicitlyConvertedBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Type = Type;
        }

        public IPythonBlock Target { get; private set; }
        public IType Type { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            return Target.GetCode();
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies();
        }
    }
}
