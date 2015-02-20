using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    /// <summary>
    /// A peephole optimization 
    /// </summary>
    public class UnboxAnyToPointerOptimization : IPeepholeOptimization
    {
        public int InstructionCount
        {
            get { return 1; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            return Instructions[0].OpCode == OpCodes.Unbox_Any;
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            Context.Emit(Context.Processor.Create(OpCodes.Unbox, (TypeReference)Instructions[0].Operand));
        }
    }
}
