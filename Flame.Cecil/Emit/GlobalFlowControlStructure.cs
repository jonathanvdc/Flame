using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class GlobalFlowControlStructure : IFlowControlStructure
    {
        public GlobalFlowControlStructure(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public ICecilBlock CreateBreak()
        {
            var blockBuilder = CodeGenerator.CreateBlock();
            blockBuilder.EmitReturn(CodeGenerator.CreateBlock());
            return (ICecilBlock)blockBuilder;
        }

        public ICecilBlock CreateContinue()
        {
            throw new NotImplementedException();
        }
    }
}
