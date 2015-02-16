using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public interface IPeepholeOptimization
    {
        /// <summary>
        /// Gets the amount of instructions the optimization intends to rewrite.
        /// </summary>
        int InstructionCount { get; }
        /// <summary>
        /// Finds out if the peephole optimization can be applied to the provided instructions.
        /// </summary>
        /// <param name="Instructions"></param>
        /// <returns></returns>
        bool IsApplicable(IReadOnlyList<Instruction> Instructions);
        /// <summary>
        /// Rewrites the provided instructions and emits the result.
        /// </summary>
        /// <param name="Instructions"></param>
        /// <param name="Context"></param>
        /// <returns></returns>
        void Rewrite(IReadOnlyList<Instruction> Instructions, IEmitContext Context);
    }
}
