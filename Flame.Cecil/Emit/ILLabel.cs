using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILLabel : ILabel
    {
        public ILLabel(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        private IEmitLabel emitLabel;
        public IEmitLabel GetEmitLabel(IEmitContext Context)
        {
            if (emitLabel == null)
            {
                emitLabel = Context.CreateLabel();
            } 
            return emitLabel;
        }

        public ICodeBlock EmitBranch(ICodeBlock Condition)
        {
            return new LabelBranchBlock(this, (ICecilBlock)Condition);
        }

        public ICodeBlock EmitMark()
        {
            return new LabelMarkBlock(this);
        }
    }
}
