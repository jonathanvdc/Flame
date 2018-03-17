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
            Codec<IType, IReadOnlyList<LNode>> typeCodec,
            Codec<IMethod, IReadOnlyList<LNode>> methodCodec)
        {
            this.InstructionCodec = instructionCodec;
            this.TypeCodec = typeCodec;
            this.MethodCodec = methodCodec;
        }

        /// <summary>
        /// Gets the encoder for instruction prototypes.
        /// </summary>
        /// <returns>The instruction prototype codec.</returns>
        public Codec<InstructionPrototype, IReadOnlyList<LNode>> InstructionCodec { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for type references.
        /// </summary>
        /// <returns>The type reference codec.</returns>
        public Codec<IType, IReadOnlyList<LNode>> TypeCodec { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for method references.
        /// </summary>
        /// <returns>The method reference codec.</returns>
        public Codec<IMethod, IReadOnlyList<LNode>> MethodCodec { get; private set; }

        /// <summary>
        /// The default codec for Flame IR as used by unmodified versions of Flame.
        /// </summary>
        public static IrCodec Default = new IrCodec(
            InstructionCodecElements.All,
            new Codec<IType, IReadOnlyList<LNode>>(),
            new Codec<IMethod, IReadOnlyList<LNode>>());
    }
}
