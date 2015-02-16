using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class DoWhileBlock : BlockBuilder
    {
        public DoWhileBlock(ICodeGenerator CodeGenerator, ICodeBlock Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
        }

        public ICodeBlock Condition { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            var brCg = (IBranchingCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var body = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, start, end);

            ((ICecilBlock)body.EmitMark()).Emit(Context);

            Context.PushFlowControl(flowStruct);

            base.Emit(Context);

            Context.PopFlowControl();

            ((ICecilBlock)start.EmitMark()).Emit(Context);
            ((ICecilBlock)body.EmitBranch(Condition)).Emit(Context);
            ((ICecilBlock)end.EmitMark()).Emit(Context);
        }
    }
}
