using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Flame.Clr.Analysis
{
    using Rewriter = Func<Instruction, IEnumerable<Instruction>>;

    /// <summary>
    /// Simplifies CIL instructions by rewriting them.
    /// </summary>
    public static class ClrInstructionSimplifier
    {
        /// <summary>
        /// Tries to "simplify" an instruction by decomposing
        /// it into its parts.
        /// </summary>
        /// <param name="instruction">The instruction to simplify.</param>
        /// <param name="simplified">The simplified instruction.</param>
        /// <returns>
        /// <c>true</c> if the instruction can be simplified;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool TrySimplify(
            Instruction instruction,
            out IEnumerable<Instruction> simplified)
        {
            Rewriter rewrite;
            if (rewritePatterns.TryGetValue(instruction.OpCode, out rewrite))
            {
                simplified = rewrite(instruction);
                return true;
            }
            else
            {
                simplified = null;
                return false;
            }
        }

        private static Dictionary<OpCode, Rewriter> rewritePatterns =
            new Dictionary<OpCode, Rewriter>()
        {
            { OpCodes.Beq, CreateConditionalBranchRewriter(OpCodes.Ceq, OpCodes.Brtrue) },
            { OpCodes.Blt, CreateConditionalBranchRewriter(OpCodes.Clt, OpCodes.Brtrue) },
            { OpCodes.Blt_Un, CreateConditionalBranchRewriter(OpCodes.Clt_Un, OpCodes.Brtrue) },
            { OpCodes.Bgt, CreateConditionalBranchRewriter(OpCodes.Cgt, OpCodes.Brtrue) },
            { OpCodes.Bgt_Un, CreateConditionalBranchRewriter(OpCodes.Cgt_Un, OpCodes.Brtrue) },
            { OpCodes.Bne_Un, CreateConditionalBranchRewriter(OpCodes.Ceq, OpCodes.Brfalse) },
            { OpCodes.Bge, CreateConditionalBranchRewriter(OpCodes.Clt, OpCodes.Brfalse) },
            { OpCodes.Bge_Un, CreateConditionalBranchRewriter(OpCodes.Clt_Un, OpCodes.Brfalse) },
            { OpCodes.Ble, CreateConditionalBranchRewriter(OpCodes.Cgt, OpCodes.Brfalse) },
            { OpCodes.Ble_Un, CreateConditionalBranchRewriter(OpCodes.Cgt_Un, OpCodes.Brfalse) },
        };

        private static Rewriter CreateConditionalBranchRewriter(
            OpCode comparisonOpCode,
            OpCode simpleBranchOpCode)
        {
            return instruction => new[]
            {
                Instruction.Create(comparisonOpCode),
                Instruction.Create(
                    simpleBranchOpCode,
                    (Instruction)instruction.Operand)
            };
        }
    }
}
