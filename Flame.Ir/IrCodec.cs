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
            Codec<InstructionPrototype, LNode> instructionCodec,
            Codec<IType, LNode> typeCodec,
            Codec<IMethod, LNode> methodCodec)
        {
            this.InstructionCodec = instructionCodec;
            this.TypeCodec = typeCodec;
            this.MethodCodec = methodCodec;
        }

        /// <summary>
        /// Gets the encoder for instruction prototypes.
        /// </summary>
        /// <returns>The instruction prototype codec.</returns>
        public Codec<InstructionPrototype, LNode> InstructionCodec { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for type references.
        /// </summary>
        /// <returns>The type reference codec.</returns>
        public Codec<IType, LNode> TypeCodec { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for method references.
        /// </summary>
        /// <returns>The method reference codec.</returns>
        public Codec<IMethod, LNode> MethodCodec { get; private set; }

        /// <summary>
        /// The default codec for Flame IR as used by unmodified versions of Flame.
        /// </summary>
        public static IrCodec Default = new IrCodec(
            InstructionCodecElements.All,
            new PiecewiseCodec<IType>(),
            new PiecewiseCodec<IMethod>());
    }
}
