using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILIfElseBlock : IIfElseBlockGenerator, IInstruction
    {
        public ILIfElseBlock(ICodeGenerator CodeGenerator, ICodeBlock Condition)
        {
            this.CodeGenerator = CodeGenerator;
            this.IfBlock = CodeGenerator.CreateBlock();
            this.ElseBlock = CodeGenerator.CreateBlock();
            this.Condition = Condition;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IBlockGenerator ElseBlock { get; private set; }
        public IBlockGenerator IfBlock { get; private set; }
        public ICodeBlock Condition { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return ((IInstruction)Condition).IsEmpty && ((IInstruction)IfBlock).IsEmpty && ((IInstruction)ElseBlock).IsEmpty;
            }
        }

        public void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            if (((IInstruction)IfBlock).IsEmpty && ((IInstruction)ElseBlock).IsEmpty)
            {
                ((IInstruction)Condition).Emit(Context, TypeStack);
                new PopInstruction(CodeGenerator).Emit(Context, TypeStack);
            }
            if (((IInstruction)IfBlock).IsEmpty)
            {
                ILabel ifLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();

                ((IInstruction)ifLabel.EmitBranch(Condition)).Emit(Context, TypeStack);
                ((IInstruction)ElseBlock).Emit(Context, TypeStack);
                ((IInstruction)ifLabel.EmitMark()).Emit(Context, TypeStack);
            }
            else if (((IInstruction)ElseBlock).IsEmpty)
            {
                ILabel elseLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();

                ((IInstruction)elseLabel.EmitBranch(CodeGenerator.EmitNot(Condition))).Emit(Context, TypeStack);
                ((IInstruction)IfBlock).Emit(Context, TypeStack);
                ((IInstruction)elseLabel.EmitMark()).Emit(Context, TypeStack);
            }
            else
            {
                ILabel ifLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();
                ILabel elseLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();

                Stack<IType> copy = new Stack<IType>();
                foreach (var item in TypeStack.Reverse())
                {
                    copy.Push(item);
                }

                ((IInstruction)elseLabel.EmitBranch(CodeGenerator.EmitNot(Condition))).Emit(Context, TypeStack);
                ((IInstruction)IfBlock).Emit(Context, TypeStack);
                ((IInstruction)ifLabel.EmitBranch(CodeGenerator.EmitBoolean(true))).Emit(Context, TypeStack);
                ((IInstruction)elseLabel.EmitMark()).Emit(Context, TypeStack);
                ((IInstruction)ElseBlock).Emit(Context, copy);
                ((IInstruction)ifLabel.EmitMark()).Emit(Context, TypeStack);

                if (TypeStack.Count != copy.Count)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
