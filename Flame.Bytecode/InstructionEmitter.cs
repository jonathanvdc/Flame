using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    /// <summary>
    /// A class that simplifies emitting instructions from an instruction buffer.
    /// </summary>
    public class InstructionEmitter
    {
        public InstructionEmitter(IBuffer<IInstruction> Instructions, int EntryPoint)
        {
            this.Instructions = Instructions;
            this.EntryPoint = EntryPoint;
        }
        public InstructionEmitter(IBuffer<IInstruction> Instructions)
            : this(Instructions, 0)
        {

        }

        /// <summary>
        /// Gets the instruction buffer this instruction parser uses.
        /// </summary>
        public IBuffer<IInstruction> Instructions { get; private set; }
        /// <summary>
        /// Gets the offset of the entry point instruction.
        /// </summary>
        public int EntryPoint { get; private set; }

        public ICodeBlock Emit(ICodeGenerator Block)
        {
            // Packing eliminates some dead code and reverses esoteric optimizations
            InstructionPacker packer = new InstructionPacker();
            packer.AddSequence(Instructions[EntryPoint], Instructions);
            var result = Block.EmitVoid();
            foreach (var item in packer.GetPackedInstructions())
            {
                result = Block.EmitSequence(result, item.Emit(Block));
            }
            return result;
        }
    }
}
