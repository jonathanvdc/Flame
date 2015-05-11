using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ThrowBlock : ICppBlock
    {
        public ThrowBlock(ICodeGenerator CodeGenerator, ICppBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICppBlock Value { get; private set; }
        public IType Type { get { return PrimitiveTypes.Void; } }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies; }
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.Append("throw ");
            cb.AppendAligned(Value.GetCode());
            cb.Append(';');
            return cb;
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
