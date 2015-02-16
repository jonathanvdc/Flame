using Flame.Compiler;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class LoadStringConstantBlock : IAssemblerBlock
    {
        public LoadStringConstantBlock(ICodeGenerator CodeGenerator, string Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public string Value { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type { get { return PrimitiveTypes.String; } }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return new IStorageLocation[] { Context.AllocateStatic(new StringExpression(Value)) };
        }
    }
}
