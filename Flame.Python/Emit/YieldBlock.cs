using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class YieldBlock : IPythonBlock
    {
        public YieldBlock(ICodeGenerator CodeGenerator, IPythonBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public IPythonBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder("yield");
            cb.Append(" ");
            cb.Append(Value.GetCode());
            return cb;
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Value.GetDependencies();
        }
    }
}
