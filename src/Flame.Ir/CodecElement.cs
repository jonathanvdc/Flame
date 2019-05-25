using System;
using System.Collections.Generic;
using Flame.Compiler;
using Loyc;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// An encoder/decoder for a specific type of object.
    /// </summary>
    public struct CodecElement<TObj, TEnc>
    {
        /// <summary>
        /// Creates a codec for a specific type of object.
        /// </summary>
        /// <param name="prototypeName">
        /// An identifier for encoded objects.
        /// </param>
        /// <param name="encode">
        /// A delegate that encodes objects.
        /// </param>
        /// <param name="decode">
        /// A delegate that decodes objects.
        /// </param>
        public CodecElement(
            string identifier,
            Func<TObj, EncoderState, TEnc> encode,
            Func<TEnc, DecoderState, TObj> decode)
            : this(GSymbol.Get(identifier), encode, decode)
        { }

        /// <summary>
        /// Creates a codec for a specific type of object.
        /// </summary>
        /// <param name="prototypeName">
        /// An identifier for encoded objects.
        /// </param>
        /// <param name="encode">
        /// A delegate that encodes objects.
        /// </param>
        /// <param name="decode">
        /// A delegate that decodes objects.
        /// </param>
        public CodecElement(
            Symbol identifier,
            Func<TObj, EncoderState, TEnc> encode,
            Func<TEnc, DecoderState, TObj> decode)
        {
            this.Identifier = identifier;
            this.Encode = encode;
            this.Decode = decode;
        }

        /// <summary>
        /// Gets an identifier for encoded objects.
        /// </summary>
        /// <returns>The encoded object identifier.</returns>
        public Symbol Identifier { get; private set; }

        /// <summary>
        /// Encodes an object's data.
        /// </summary>
        /// <returns>A delegate that encodes objects.</returns>
        public Func<TObj, EncoderState, TEnc> Encode { get; private set; }

        /// <summary>
        /// Decodes an object's data.
        /// </summary>
        /// <returns>A delegate that decodes objects.</returns>
        public Func<TEnc, DecoderState, TObj> Decode { get; private set; }
    }
}
