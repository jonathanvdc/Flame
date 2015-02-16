using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    public static class InstructionExtensions
    {
        public static bool ContainsOffset(this IInstruction Instruction, int Offset)
        {
            return Offset >= Instruction.Offset && Offset < Instruction.Offset + Instruction.Size;
        }
        /// <summary>
        /// Gets a boolean value that indicates whether the second instruction follows the first instruction immediately in the instruction buffer.
        /// </summary>
        /// <param name="Instruction"></param>
        /// <param name="Other"></param>
        /// <returns></returns>
        public static bool IsNext(this IInstruction Instruction, IInstruction Other)
        {
            return Other.Offset == Instruction.Offset + Instruction.Size;
        }
        /// <summary>
        /// Gets a boolean value that indicates whether the first instruction follows the second instruction immediately in the instruction buffer.
        /// </summary>
        /// <param name="Instruction"></param>
        /// <param name="Other"></param>
        /// <returns></returns>
        public static bool IsPrevious(this IInstruction Instruction, IInstruction Other)
        {
            return Other.IsNext(Instruction);
        }
        public static bool Overlaps(this IInstruction Instruction, IInstruction Other)
        {
            return Instruction.ContainsOffset(Other.Offset) || Other.ContainsOffset(Instruction.Offset);
        }
    }
}
