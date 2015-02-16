using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ComparisonBranchOptimization : IPeepholeOptimization
    {
        public ComparisonBranchOptimization(IEmitLabel Label)
        {
            this.Label = Label;
        }

        public IEmitLabel Label { get; private set; }

        public int InstructionCount
        {
            get { return 1; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            return Instructions[0].OpCode == OpCodes.Ceq || Instructions[0].OpCode == OpCodes.Clt || Instructions[0].OpCode == OpCodes.Clt_Un || Instructions[0].OpCode == OpCodes.Cgt || Instructions[0].OpCode == OpCodes.Cgt_Un;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            if (Instructions[0].OpCode == OpCodes.Ceq)
            {
                Context.Emit(OpCodes.Beq, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Clt)
            {
                Context.Emit(OpCodes.Blt, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Clt_Un)
            {
                Context.Emit(OpCodes.Blt_Un, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Cgt)
            {
                Context.Emit(OpCodes.Bgt, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Cgt_Un)
            {
                Context.Emit(OpCodes.Bgt_Un, Label);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
