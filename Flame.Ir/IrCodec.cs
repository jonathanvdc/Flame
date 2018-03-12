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
        /// Creates a codec for Flame IR from a number of sub-codecs.
        /// </summary>
        /// <param name="instructionCodec">An instruction prototype codec.</param>
        /// <param name="typeCodec">A type codec.</param>
        public IrCodec(
            Codec<InstructionPrototype, IReadOnlyList<LNode>> instructionCodec,
            Codec<IType, IReadOnlyList<LNode>> typeCodec)
        {
            this.InstructionCodec = instructionCodec;
            this.TypeCodec = typeCodec;
        }

        /// <summary>
        /// Gets the encoder for instruction prototypes.
        /// </summary>
        /// <returns>The instruction prototype codec.</returns>
        public Codec<InstructionPrototype, IReadOnlyList<LNode>> InstructionCodec { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for types.
        /// </summary>
        /// <returns>The type codec.</returns>
        public Codec<IType, IReadOnlyList<LNode>> TypeCodec { get; private set; }
    }
}
