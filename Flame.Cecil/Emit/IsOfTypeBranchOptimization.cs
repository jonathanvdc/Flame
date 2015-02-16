using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class IsOfTypeBranchOptimization : IPeepholeOptimization
    {
        public IsOfTypeBranchOptimization(IEmitLabel Label)
        {
            this.Label = Label;
        }

        public IEmitLabel Label { get; private set; }

        public int InstructionCount
        {
            get { return 2; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            return Instructions[0].OpCode == OpCodes.Ldnull && Instructions[1].OpCode == OpCodes.Cgt_Un;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            Context.Emit(OpCodes.Ldnull); // Re-emit ldnull
            Context.Emit(OpCodes.Bne_Un, Label);
        }
    }

    public class IsNotOfTypeBranchOptimization : IPeepholeOptimization
    {
        public IsNotOfTypeBranchOptimization(IEmitLabel Label)
        {
            this.Label = Label;
        }

        public IEmitLabel Label { get; private set; }

        public int InstructionCount
        {
            get { return 4; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            return Instructions[0].OpCode == OpCodes.Ldnull && Instructions[1].OpCode == OpCodes.Cgt_Un && 
                Instructions[2].OpCode == OpCodes.Ldc_I4_0 && Instructions[3].OpCode == OpCodes.Ceq;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            Context.Emit(OpCodes.Ldnull); // Re-emit ldnull
            Context.Emit(OpCodes.Beq, Label);
        }
    }
}
