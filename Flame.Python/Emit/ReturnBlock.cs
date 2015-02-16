using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class ReturnBlock : IPythonBlock
    {
        public ReturnBlock(ICodeGenerator CodeGenerator, IPythonBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public IPythonBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder("return");
            if (Value != null)
            {
                cb.Append(" ");
                cb.Append(Value.GetCode());
            }
            return cb;
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return (Value ?? new PythonNonexistantBlock(CodeGenerator)).GetDependencies();
        }
    }
}
