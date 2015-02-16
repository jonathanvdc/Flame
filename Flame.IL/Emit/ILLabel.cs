using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILLabel : ILabel
    {
        public ILLabel(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator; 
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IEmitLabel EmitLabel { get; private set; }

        public void Bind(ICommandEmitContext Context)
        {
            if (this.EmitLabel == null)
            {
                this.EmitLabel = Context.CreateLabel();
            }
        }

        public ICodeBlock EmitBranch(ICodeBlock Condition)
        {
            return new BranchInstruction(CodeGenerator, this, Condition);
        }

        public ICodeBlock EmitMark()
        {
            return new MarkInstruction(CodeGenerator, this);
        }
    }
}
