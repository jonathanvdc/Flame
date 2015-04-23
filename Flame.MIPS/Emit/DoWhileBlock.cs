using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class DoWhileBlock : IAssemblerBlock
    {
        public DoWhileBlock(ICodeGenerator CodeGenerator, IAssemblerBlock Condition, IAssemblerBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.Body = Body;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerBlock Condition { get; private set; }
        public IAssemblerBlock Body { get; private set; }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var brCg = (IBranchingCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var body = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, start, end);

            ((IAssemblerBlock)body.EmitMark()).Emit(Context);

            Context.FlowControl.Push(flowStruct);

            var results = Body.Emit(Context);

            Context.FlowControl.Pop();

            ((IAssemblerBlock)start.EmitMark()).Emit(Context);
            ((IAssemblerBlock)body.EmitBranch(Condition)).Emit(Context);
            ((IAssemblerBlock)end.EmitMark()).Emit(Context);

            return results;
        }

        public IType Type
        {
            get { return Body.Type; }
        }
    }
}
