using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BranchFlowStructure : IFlowControlStructure
    {
        public BranchFlowStructure(ICodeGenerator CodeGenerator, UniqueTag Tag, ILabel Start, ILabel End)
        {
            this.CodeGenerator = CodeGenerator;
            this.Start = Start;
            this.End = End;
            this.Tag = Tag;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public UniqueTag Tag { get; private set; }
        public ILabel Start { get; private set; }
        public ILabel End { get; private set; }

        public ICecilBlock CreateBreak()
        {
            return (ICecilBlock)End.EmitBranch(CodeGenerator.EmitBoolean(true));
        }

        public ICecilBlock CreateContinue()
        {
            return (ICecilBlock)Start.EmitBranch(CodeGenerator.EmitBoolean(true));
        }
    }
}
