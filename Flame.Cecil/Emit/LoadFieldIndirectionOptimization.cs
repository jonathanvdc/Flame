using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    /// <summary>
    /// A peephole optimization that tackles loading fields from dereferenced pointers.
    /// </summary>
    public class LoadFieldIndirectionOptimization : IPeepholeOptimization
    {
        public int InstructionCount
        {
            get { return 1; }
        }

        public bool IsApplicable(IReadOnlyList<Instruction> Instructions)
        {
            return Instructions[0].OpCode.IsDereferencePointerOpCode();
        }

        public void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context)
        {
            // Do nothing to get rid of the dereferencing instruction.
        }
    }
}
