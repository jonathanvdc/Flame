using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ReturnBlock : ICppBlock
    {
        public ReturnBlock(ICodeGenerator CodeGenerator, ICppBlock Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = ConversionBlock.Cast(Value, CodeGenerator.Method.ReturnType);
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICppBlock Value { get; private set; }
        public IType Type { get { return PrimitiveTypes.Void; } }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value == null ? Enumerable.Empty<IHeaderDependency>() : Value.Dependencies; }
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.Append("return");
            if (Value != null)
            {
                cb.Append(' ');
                cb.AppendAligned(Value.GetCode());
            }
            cb.Append(';');
            return cb;
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value == null ? Enumerable.Empty<CppLocal>() : Value.LocalsUsed; }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
