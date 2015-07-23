using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class WhileBlock : ICecilBlock
    {
        public WhileBlock(ICodeGenerator CodeGenerator, BlockTag Tag, ICecilBlock Condition, ICecilBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Tag = Tag;
            this.Condition = Condition;
            this.Body = Body;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public BlockTag Tag { get; private set; }
        public ICecilBlock Condition { get; private set; }
        public ICecilBlock Body { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var brCg = (IBranchingCodeGenerator)CodeGenerator;
            var start = brCg.CreateLabel();
            var body = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            var flowStruct = new BranchFlowStructure(CodeGenerator, Tag, start, end);

            flowStruct.CreateContinue().Emit(Context);
            ((ICecilBlock)body.EmitMark()).Emit(Context);

            Context.PushFlowControl(flowStruct);

            Body.Emit(Context);

            Context.PopFlowControl();

            ((ICecilBlock)start.EmitMark()).Emit(Context);
            ((ICecilBlock)body.EmitBranch(Condition)).Emit(Context);
            ((ICecilBlock)end.EmitMark()).Emit(Context);
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }
    }

    public class BranchFlowStructure : IFlowControlStructure
    {
        public BranchFlowStructure(ICodeGenerator CodeGenerator, BlockTag Tag, ILabel Start, ILabel End)
        {
            this.CodeGenerator = CodeGenerator;
            this.Start = Start;
            this.End = End;
            this.Tag = Tag;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public BlockTag Tag { get; private set; }
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
