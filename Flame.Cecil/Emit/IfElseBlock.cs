using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class IfElseBlock : ICecilBlock
    {
        public IfElseBlock(ICodeGenerator CodeGenerator, ICecilBlock Condition, ICecilBlock IfBlock, ICecilBlock ElseBlock)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.IfBlock = IfBlock;
            this.ElseBlock = ElseBlock;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Condition { get; private set; }
        public ICecilBlock IfBlock { get; private set; }
        public ICecilBlock ElseBlock { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var brCg = (IBranchingCodeGenerator)CodeGenerator;

            if (ElseBlock.IsEmpty()) // Optimize for this case, as it is quite common.
            {
                var end = brCg.CreateLabel();

                ((ICecilBlock)end.EmitBranch(CodeGenerator.EmitNot(Condition))).Emit(Context);
                ((ICecilBlock)IfBlock).Emit(Context);
                ((ICecilBlock)end.EmitMark()).Emit(Context);
            }
            else if (IfBlock.IsEmpty()) // This case is not quite as common, but we might as well optimize
                                        // for the sake symmetry.
            {
                var end = brCg.CreateLabel();

                ((ICecilBlock)end.EmitBranch(Condition)).Emit(Context);
                ((ICecilBlock)ElseBlock).Emit(Context);
                ((ICecilBlock)end.EmitMark()).Emit(Context);
            }
            else
            {
                var elseBlock = brCg.CreateLabel();
                var end = brCg.CreateLabel();

                ((ICecilBlock)elseBlock.EmitBranch(CodeGenerator.EmitNot(Condition))).Emit(Context);
                ((ICecilBlock)IfBlock).Emit(Context);
                ((ICecilBlock)end.EmitBranch(CodeGenerator.EmitBoolean(true))).Emit(Context);
                ((ICecilBlock)elseBlock.EmitMark()).Emit(Context);
                ((ICecilBlock)ElseBlock).Emit(Context);
                ((ICecilBlock)end.EmitMark()).Emit(Context);
            }
        }

        public IType BlockType
        {
            get { return IfBlock.BlockType; }
        }
    }
}
