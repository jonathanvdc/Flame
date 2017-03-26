using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class TaggedBlock : IAssemblerBlock
    {
        public TaggedBlock(ICodeGenerator CodeGenerator, UniqueTag Tag, IAssemblerBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
            this.Body = Body;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public UniqueTag Tag { get; private set; }
        public IAssemblerBlock Body { get; private set; }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var brCg = (AssemblerCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, Tag, start, end);

            Context.FlowControl.Push(flowStruct);

            ((IAssemblerBlock)start.EmitMark()).Emit(Context);
            var results = Body.Emit(Context);
            ((IAssemblerBlock)end.EmitMark()).Emit(Context);

            Context.FlowControl.Pop();

            return results;
        }

        public IType Type
        {
            get { return Body.Type; }
        }
    }
}
