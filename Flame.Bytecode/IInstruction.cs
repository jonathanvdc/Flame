using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    /// <summary>
    /// Presents common instruction functionality.
    /// </summary>
    public interface IInstruction : IEquatable<IInstruction>
    {
        /// <summary>
        /// Gets the instruction's offset.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Gets the instruction's size.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets all possible instructions that can be executed immediately after this instruction.
        /// </summary>
        IEnumerable<IInstruction> GetNext(IBuffer<IInstruction> Instructions);

        /// <summary>
        /// Emits the instruction to a block generator.
        /// </summary>
        /// <param name="Target"></param>
        void Emit(IBlockGenerator Target);
    }
}
