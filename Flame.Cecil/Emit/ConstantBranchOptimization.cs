using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ConstantBranchOptimization : IPeepholeOptimization
    {
        public ConstantBranchOptimization(IEmitLabel Label)
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
            return Instructions[0].OpCode == OpCodes.Ldc_I4_1 || Instructions[0].OpCode == OpCodes.Ldc_I4_0;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            if (Instructions[0].OpCode == OpCodes.Ldc_I4_1)
            {
                Context.Emit(OpCodes.Br, Label);
            }
        }
    }
}
