using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Compiler.Emit
{
    public class BranchingIfElseBlock : CompositeBlock, IIfElseBlockGenerator
    {
        public BranchingIfElseBlock(IBranchingCodeGenerator CodeGenerator, ICodeBlock Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
            this.IfBlock = CodeGenerator.CreateBlock();
            this.ElseBlock = CodeGenerator.CreateBlock();
        }

        public ICodeBlock Condition { get; private set; }
        public IBlockGenerator IfBlock { get; private set; }
        public IBlockGenerator ElseBlock { get; private set; }

        public override ICodeBlock Peel()
        {
            var block = CodeGenerator.CreateBlock();
            var ifLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();
            var elseLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();
            block.EmitBlock(ifLabel.EmitBranch(Condition));
            block.EmitBlock(ElseBlock);
            block.EmitBlock(elseLabel.EmitBranch(CodeGenerator.EmitBoolean(true)));
            block.EmitBlock(ifLabel.EmitMark());
            block.EmitBlock(IfBlock);
            block.EmitBlock(elseLabel.EmitMark());
            return block;
        }


    }
}
