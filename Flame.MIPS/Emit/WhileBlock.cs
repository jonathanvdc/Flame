using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class WhileBlock : AssemblerBlockGenerator
    {
        public WhileBlock(ICodeGenerator CodeGenerator, ICodeBlock Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
        }

        public ICodeBlock Condition { get; private set; }

        public override IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var brCg = (IBranchingCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var body = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, start, end);

            flowStruct.EmitContinue().Emit(Context);
            ((IAssemblerBlock)body.EmitMark()).Emit(Context);

            Context.FlowControl.Push(flowStruct);

            var results = base.Emit(Context);

            Context.FlowControl.Pop();

            ((IAssemblerBlock)start.EmitMark()).Emit(Context);
            ((IAssemblerBlock)body.EmitBranch(Condition)).Emit(Context);
            ((IAssemblerBlock)end.EmitMark()).Emit(Context);

            return results;
        }
    }
    public class BranchFlowStructure : IFlowControlStructure
    {
        public BranchFlowStructure(ICodeGenerator CodeGenerator, ILabel Start, ILabel End)
        {
            this.CodeGenerator = CodeGenerator;
            this.Start = Start;
            this.End = End;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
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
