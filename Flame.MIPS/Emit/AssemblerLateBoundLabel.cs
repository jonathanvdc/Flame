using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerLateBoundLabel
    {
        public AssemblerLateBoundLabel(ICodeGenerator CodeGenerator)
        {
            this.Name = null;
            this.CodeGenerator = CodeGenerator;
        }
        public AssemblerLateBoundLabel(ICodeGenerator CodeGenerator, string Name)
        {
            this.Name = Name;
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public string Name { get; private set; }

        private IAssemblerLabel label;
        public IAssemblerLabel Bind(IAssemblerEmitContext Context)
        {
            if (label == null)
            {
                label = Context.DeclareLabel(Name);
            }
            return label;
        }

        public ICodeBlock EmitBranch(ICodeBlock Condition)
        {
            return new BranchBlock(this, (IAssemblerBlock)Condition);
        }

        public ICodeBlock EmitMark()
        {
            return new MarkLabelBlock(this);
        }
    }
}
