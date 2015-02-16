using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BooleanBranchOptimization : IPeepholeOptimization
    {
        public BooleanBranchOptimization(IEmitLabel Label)
        {
            this.Label = Label;
            this.IsBooleanComparison = false;
        }
        public BooleanBranchOptimization(IEmitLabel Label, bool IsBooleanComparison)
        {
            this.Label = Label;
            this.IsBooleanComparison = IsBooleanComparison;
        }

        public IEmitLabel Label { get; private set; }
        public bool IsBooleanComparison { get; private set; }

        public int InstructionCount
        {
            get { return 2; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            if (!IsBooleanComparison)
            {
                return Instructions[1].OpCode == OpCodes.Ceq && Instructions[0].OpCode == OpCodes.Ldc_I4_0;
            }
            else
            {
                return Instructions[1].OpCode == OpCodes.Ceq && (Instructions[0].OpCode == OpCodes.Ldc_I4_1 || Instructions[0].OpCode == OpCodes.Ldc_I4_0);
            }
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            if (Instructions[0].OpCode == OpCodes.Ldc_I4_1)
            {
                Context.Emit(OpCodes.Brtrue, Label);
            }
            else
            {
                Context.Emit(OpCodes.Brfalse, Label);
            }
        }
    }
}
