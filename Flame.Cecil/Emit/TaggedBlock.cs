using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class TaggedBlock : ICecilBlock
    {
        public TaggedBlock(ICodeGenerator CodeGenerator, UniqueTag Tag, ICecilBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
            this.Body = Body;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public UniqueTag Tag { get; private set; }
        public ICecilBlock Body { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var brCg = (ILCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, Tag, start, end);

            Context.PushFlowControl(flowStruct);

            ((ICecilBlock)start.EmitMark()).Emit(Context);
            Body.Emit(Context);
            ((ICecilBlock)end.EmitMark()).Emit(Context);

            Context.PopFlowControl();
        }

        public IType BlockType
        {
            get { return Body.BlockType; }
        }
    }
}
