using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class NotOptimization : IPeepholeOptimization
    {
        public int InstructionCount
        {
            get { return 1; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            return Instructions[0].OpCode == OpCodes.Not;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
        }
    }
}
