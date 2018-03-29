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
        /// <param name="constants">A codec for constants.</param>
        /// <param name="instructionCodec">An instruction prototype codec.</param>
        /// <param name="typeCodec">A codec for types.</param>
        /// <param name="methodCodec">A codec for methods.</param>
        public IrCodec(
            Codec<Constant, LNode> constants,
            Codec<InstructionPrototype, LNode> instructionCodec,
            Codec<IType, LNode> typeCodec,
            Codec<IMethod, LNode> methodCodec)
        {
            this.Constants = constants;
            this.Instructions = instructionCodec;
            this.Types = typeCodec;
            this.Methods = methodCodec;
        }

        /// <summary>
        /// Gets the encoder for constants.
        /// </summary>
        /// <returns>The constant codec.</returns>
        public Codec<Constant, LNode> Constants { get; private set; }

        /// <summary>
        /// Gets the encoder for instruction prototypes.
        /// </summary>
        /// <returns>The instruction prototype codec.</returns>
        public Codec<InstructionPrototype, LNode> Instructions { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for type references.
        /// </summary>
        /// <returns>The type reference codec.</returns>
        public Codec<IType, LNode> Types { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for method references.
        /// </summary>
        /// <returns>The method reference codec.</returns>
        public Codec<IMethod, LNode> Methods { get; private set; }

        /// <summary>
        /// The default codec for Flame IR as used by unmodified versions of Flame.
        /// </summary>
        public static IrCodec Default = new IrCodec(
            ConstantCodec.Instance,
            InstructionCodecElements.All,
            TypeCodec.Instance,
            new PiecewiseCodec<IMethod>());
    }
}
