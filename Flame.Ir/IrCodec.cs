using System.Collections.Generic;
using Flame.Compiler;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// An encoder/decoder for every configurable element of Flame's
    /// intermediate representation.
    /// </summary>
    public struct IrCodec
    {
        /// <summary>
        /// Creates a codec for Flame IR.
        /// </summary>
        /// <param name="instructionCodec"></param>
        public IrCodec(
            Codec<InstructionPrototype, IReadOnlyList<LNode>> instructionCodec)
        {
            this.InstructionCodec = instructionCodec;
        }

        /// <summary>
        /// Gets the encoder for instruction prototypes.
        /// </summary>
        /// <returns>The instruction prototype codec.</returns>
        public Codec<InstructionPrototype, IReadOnlyList<LNode>> InstructionCodec { get; private set; }
    }
}