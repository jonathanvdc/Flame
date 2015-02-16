using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class StringConstant : IPythonBlock
    {
        public StringConstant(ICodeGenerator CodeGenerator, string Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public string Value { get; private set; }

        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            var sb = new StringBuilder(Value);
            sb.Replace("\\", "\\\\");
            sb.Replace("\t", "\\t");
            sb.Replace("\n", "\\n");
            sb.Replace("\r", "\\r");
            sb.Replace("\0", "\\0");
            return new CodeBuilder("\"" + sb.ToString() + "\"");
        }

        public IType Type
        {
            get { return PrimitiveTypes.String; }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return new ModuleDependency[0];
        }
    }
}
