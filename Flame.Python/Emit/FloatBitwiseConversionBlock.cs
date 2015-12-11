using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;

namespace Flame.Python.Emit
{
    public class FloatBitwiseConversionBlock : IPythonBlock
    {
        public FloatBitwiseConversionBlock(ICodeGenerator CodeGenerator, IPythonBlock Value, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Value = Value;
        }

        public IPythonBlock Value { get; private set; }
        public IType Type { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            int size = Value.Type.GetPrimitiveSize();
            string intName = size == 4 ? "l" : "q";
            string floatName = size == 4 ? "f" : "d";
            string targetName, sourceName;
            if (Type.GetIsBit())
            {
                targetName = intName;
                sourceName = floatName;
            }
            else
            {
                targetName = floatName;
                sourceName = intName;
            }
            CodeBuilder cb = new CodeBuilder();
            cb.Append("unpack('" + targetName + "', pack('" + sourceName + "', ");
            cb.Append(Value.GetCode());
            cb.Append("))[0]");
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Value.GetDependencies();
        }
    }
}
