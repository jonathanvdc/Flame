using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class DoWhileBlock : AssemblerBlockGenerator
    {
        public DoWhileBlock(ICodeGenerator CodeGenerator, IAssemblerBlock Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
        }

        public IAssemblerBlock Condition { get; private set; }

        public override IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var brCg = (IBranchingCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var body = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, start, end);

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
}
