using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonCodeBlock : IPythonBlock
    {
        public PythonCodeBlock(ICodeGenerator CodeGenerator, IType Type, CodeBuilder Code)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Code = Code;
        }

        public CodeBuilder Code { get; private set; }
        public IType Type { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            var newCb = new CodeBuilder();
            newCb.Append(Code);
            return newCb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return new ModuleDependency[0];
        }
    }
}
