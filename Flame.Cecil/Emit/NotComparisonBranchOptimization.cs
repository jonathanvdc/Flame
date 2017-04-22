using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class NonNullBranchOptimization : IPeepholeOptimization
    {
        public NonNullBranchOptimization(IEmitLabel Label)
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
            return Instructions[0].OpCode == OpCodes.Ldnull
                && Instructions[1].OpCode == OpCodes.Ceq
                && Instructions[2].OpCode == OpCodes.Ldc_I4_0
                && Instructions[3].OpCode == OpCodes.Ceq;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            Context.Emit(OpCodes.Brtrue, Label);
        }
    }

    public class NotComparisonBranchOptimization : IPeepholeOptimization
    {
        public NotComparisonBranchOptimization(IEmitLabel Label)
        {
            this.Label = Label;
        }

        public IEmitLabel Label { get; private set; }

        public int InstructionCount
        {
            get { return 3; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            if (Instructions[1].OpCode == OpCodes.Ldc_I4_0 && Instructions[2].OpCode == OpCodes.Ceq)
            {
                return Instructions[0].OpCode == OpCodes.Ceq
                    || Instructions[0].OpCode == OpCodes.Clt
                    || Instructions[0].OpCode == OpCodes.Clt_Un
                    || Instructions[0].OpCode == OpCodes.Cgt
                    || Instructions[0].OpCode == OpCodes.Cgt_Un;
            }
            else
            {
                return false;
            }
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            if (Instructions[0].OpCode == OpCodes.Ceq)
            {
                Context.Emit(OpCodes.Bne_Un, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Clt)
            {
                Context.Emit(OpCodes.Bge, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Clt_Un)
            {
                Context.Emit(OpCodes.Bge_Un, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Cgt)
            {
                Context.Emit(OpCodes.Ble, Label);
            }
            else if (Instructions[0].OpCode == OpCodes.Cgt_Un)
            {
                Context.Emit(OpCodes.Ble_Un, Label);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
