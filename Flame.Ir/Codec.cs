using System;
using System.Collections.Immutable;
using Loyc;

namespace Flame.Ir
{
    /// <summary>
    /// An encoder/decoder for values.
    /// </summary>
    public abstract class Codec<TObj, TEnc>
    {
        /// <summary>
        /// Encodes a value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="state">The state of the encoder.</param>
        /// <returns>The encoded value.</returns>
        public abstract TEnc Encode(TObj value, EncoderState state);

        /// <summary>
        /// Decodes a particular piece of data.
        /// </summary>
        /// <param name="identifier">
        /// A symbol that identifies the type of data that is encoded.
        /// </param>
        /// <param name="data">
        /// Encoded data to decode.
        /// </param>
        /// <param name="state">
        /// The decoder's state.
        /// </param>
        /// <returns>
        /// A decoded object.
        /// </returns>
        public abstract TObj Decode(TEnc data, DecoderState state);
    }
}
