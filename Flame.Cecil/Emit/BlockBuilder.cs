using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BlockBuilder : ICecilBlock, IBlockGenerator
    {
        public BlockBuilder(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Blocks = new List<ICecilBlock>();
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public List<ICecilBlock> Blocks { get; private set; }

        public void EmitBlock(ICodeBlock Block)
        {
            Blocks.Add((ICecilBlock)Block);
        }

        public void EmitBreak()
        {
            Blocks.Add(new BreakBlock(CodeGenerator));
        }

        public void EmitContinue()
        {
            Blocks.Add(new ContinueBlock(CodeGenerator));
        }

        public void EmitPop(ICodeBlock Block)
        {
            Blocks.Add(new PopBlock(CodeGenerator, (ICecilBlock)Block));
        }

        public void EmitReturn(ICodeBlock Block)
        {
            Blocks.Add(new ReturnBlock(CodeGenerator, (ICecilBlock)Block));
        }

        public virtual void Emit(IEmitContext Context)
        {
            foreach (var item in Blocks)
            {
                item.Emit(Context);
            }
        }

        public IStackBehavior StackBehavior
        {
            get { return new BlockStackBehavior(Blocks.Select((item) => item.StackBehavior).ToArray()); }
        }
    }
}
