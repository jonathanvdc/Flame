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
            return (ICecilBlock)CodeGenerator.EmitReturn(CodeGenerator.EmitVoid());
        }

        public ICecilBlock CreateContinue()
        {
            throw new NotImplementedException();
        }
    }
}
