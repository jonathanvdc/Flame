using System.Collections.Generic;
using Flame.Compiler;
using Loyc;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// Encodes Flame's intermediate representation as Loyc LNodes.
    /// </summary>
    public sealed class EncoderState
    {
        /// <summary>
        /// Instantiates a Flame IR encoder.
        /// </summary>
        /// <param name="codec">The codec to use for encoding.</param>
        /// <param name="factory">The node factory to use for creating nodes.</param>
        public EncoderState(IrCodec codec, LNodeFactory factory)
        {
            this.Codec = codec;
            this.Factory = factory;
        }

        /// <summary>
        /// Gets the codec used by this encoder.
        /// </summary>
        /// <returns>A Flame IR codec.</returns>
        public IrCodec Codec { get; private set; }

        /// <summary>
        /// Gets the node factory for this encoder.
        /// </summary>
        /// <returns>A node factory.</returns>
        public LNodeFactory Factory { get; private set; }

        private LNode Encode<T>(T value, Codec<T, IReadOnlyList<LNode>> codec)
        {
            Symbol identifier;
            var args = codec.Encode(value, this, out identifier);
            return Factory.Call(Factory.Id(identifier), args);
        }

        /// <summary>
        /// Encodes a type.
        /// </summary>
        /// <param name="type">The type to encode.</param>
        /// <returns>
        /// An encoded type.
        /// </returns>
        public LNode Encode(IType type)
        {
            return Encode<IType>(type, Codec.TypeCodec);
        }

        /// <summary>
        /// Encodes an instruction prototype.
        /// </summary>
        /// <param name="prototype">The instruction prototype to encode.</param>
        /// <returns>
        /// An encoded instruction prototype.
        /// </returns>
        public LNode Encode(InstructionPrototype prototype)
        {
            return Encode<InstructionPrototype>(prototype, Codec.InstructionCodec);
        }
    }
}
