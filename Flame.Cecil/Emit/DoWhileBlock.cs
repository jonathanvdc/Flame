using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class DoWhileBlock : ICecilBlock
    {
        public DoWhileBlock(ICodeGenerator CodeGenerator, ICecilBlock Condition, ICecilBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.Body = Body;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Condition { get; private set; }
        public ICecilBlock Body { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var brCg = (IBranchingCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var body = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, start, end);

            ((ICecilBlock)body.EmitMark()).Emit(Context);

            Context.PushFlowControl(flowStruct);

            Body.Emit(Context);

            Context.PopFlowControl();

            ((ICecilBlock)start.EmitMark()).Emit(Context);
            ((ICecilBlock)body.EmitBranch(Condition)).Emit(Context);
            ((ICecilBlock)end.EmitMark()).Emit(Context);
        }

        public IStackBehavior StackBehavior
        {
            get { return new PopStackBehavior(0); }
        }
    }
}
