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
        public BranchFlowStructure(
            ICodeGenerator CodeGenerator, UniqueTag Tag,
            AssemblerLateBoundLabel Start, AssemblerLateBoundLabel End)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
            this.Start = Start;
            this.End = End;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public UniqueTag Tag { get; private set; }
        public AssemblerLateBoundLabel Start { get; private set; }
        public AssemblerLateBoundLabel End { get; private set; }

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
