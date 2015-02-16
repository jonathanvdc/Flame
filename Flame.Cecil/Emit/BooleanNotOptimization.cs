using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class BooleanNotOptimization : IPeepholeOptimization
    {
        public int InstructionCount
        {
            get { return 2; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            return Instructions[0].OpCode == OpCodes.Ldc_I4_0 && Instructions[1].OpCode == OpCodes.Ceq;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
        }
    }
}
