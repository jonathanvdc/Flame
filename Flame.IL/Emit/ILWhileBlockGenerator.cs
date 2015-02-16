using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILWhileBlockGenerator : ILBlockGenerator, IFlowBlockGenerator
    {
        public ILWhileBlockGenerator(ICodeGenerator CodeGenerator, ICodeBlock Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
            this.nextLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();
            this.exitLabel = ((IBranchingCodeGenerator)CodeGenerator).CreateLabel();
        }

        public override bool IsEmpty
        {
            get
            {
                return ((IInstruction)Condition).IsEmpty && base.IsEmpty;
            }
        }

        public ICodeBlock Condition { get; private set; }
        private ILabel nextLabel;
        private ILabel exitLabel;

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            ((IInstruction)nextLabel.EmitMark()).Emit(Context, TypeStack);
            ((IInstruction)exitLabel.EmitBranch(CodeGenerator.EmitNot(Condition))).Emit(Context, TypeStack);
            base.Emit(Context, TypeStack);
            ((IInstruction)nextLabel.EmitBranch(CodeGenerator.EmitBoolean(true))).Emit(Context, TypeStack);
            ((IInstruction)exitLabel.EmitMark()).Emit(Context, TypeStack);
        }

        public void EmitBreak()
        {
            EmitBlock(exitLabel.EmitBranch(CodeGenerator.EmitBoolean(true)));
        }

        public void EmitContinue()
        {
            EmitBlock(nextLabel.EmitBranch(CodeGenerator.EmitBoolean(true)));
        }
    }
}
