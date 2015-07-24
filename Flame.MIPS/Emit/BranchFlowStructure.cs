using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class BranchFlowStructure : IFlowControlStructure
    {
        public BranchFlowStructure(ICodeGenerator CodeGenerator, BlockTag Tag, ILabel Start, ILabel End)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
            this.Start = Start;
            this.End = End;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public BlockTag Tag { get; private set; }
        public ILabel Start { get; private set; }
        public ILabel End { get; private set; }

        public IAssemblerBlock EmitBreak()
        {
            return (IAssemblerBlock)End.EmitBranch(CodeGenerator.EmitBoolean(true));
        }

        public IAssemblerBlock EmitContinue()
        {
            return (IAssemblerBlock)Start.EmitBranch(CodeGenerator.EmitBoolean(true));
        }
    }
}
