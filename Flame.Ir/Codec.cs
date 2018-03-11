using System;
using System.Collections.Immutable;
using Loyc;

namespace Flame.Ir
{
    /// <summary>
    /// An encoder/decoder for a broad class of values that uses identifiers
    /// to differentiate between different types of encoded objects.
    /// </summary>
    public sealed class Codec<TObj, TEnc>
    {
        /// <summary>
        /// Creates an empty codec.
        /// </summary>
        public Codec()
            : this(
                ImmutableDictionary<Symbol, Func<TEnc, DecoderState, TObj>>.Empty,
                ImmutableDictionary<Type, Func<TObj, EncoderState, TEnc>>.Empty,
                ImmutableDictionary<Type, Symbol>.Empty)
        { }

        private Codec(
            ImmutableDictionary<Symbol, Func<TEnc, DecoderState, TObj>> decoders,
            ImmutableDictionary<Type, Func<TObj, EncoderState, TEnc>> encoders,
            ImmutableDictionary<Type, Symbol> identifiers)
        {
            this.decoders = decoders;
            this.encoders = encoders;
            this.identifiers = identifiers;
        }

        private ImmutableDictionary<Symbol, Func<TEnc, DecoderState, TObj>> decoders;

        private ImmutableDictionary<Type, Func<TObj, EncoderState, TEnc>> encoders;

        private ImmutableDictionary<Type, Symbol> identifiers;

        /// <summary>
        /// Adds an encoder for a specific type of element to this more
        /// general codec.
        /// </summary>
        /// <param name="element">A codec for a particular type of element.</param>
        /// <returns>
        /// A new codec that can encode and decode elements of type <typeparamref name="T"/>.
        /// </returns>
        public Codec<TObj, TEnc> Add<T>(CodecElement<T, TEnc> element)
            where T : TObj
        {
            var newDecoders = decoders.Add(
                element.Identifier,
                (data, state) => element.Decode(data, state));

            var newEncoders = encoders.Add(
                typeof(T),
                (obj, state) => element.Encode((T)obj, state));

            var newIdentifiers = identifiers.Add(typeof(T), element.Identifier);

            return new Codec<TObj, TEnc>(newDecoders, newEncoders, newIdentifiers);
        }

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
        public TObj Decode(Symbol identifier, TEnc data, DecoderState state)
        {
            return decoders[identifier](data, state);
        }

        /// <summary>
        /// Encodes a value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="state">The state of the encoder.</param>
        /// <returns>The encoded value.</returns>
        public TEnc Encode(TObj value, EncoderState state)
        {
            return encoders[value.GetType()](value, state);
        }

        /// <summary>
        /// Encodes a value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="state">The state of the encoder.</param>
        /// <param name="identifier">The identifier for the encoded value.</param>
        /// <returns>The encoded value.</returns>
        public TEnc Encode(TObj value, EncoderState state, out Symbol identifier)
        {
            var type = value.GetType();
            identifier = identifiers[type];
            return encoders[type](value, state);
        }

        /// <summary>
        /// Gets the identifier a value would have if it were encoded.
        /// </summary>
        /// <param name="value">
        /// A value to inspect.
        /// </param>
        /// <returns>
        /// An identifier.
        /// </returns>
        public Symbol GetIdentifier(TObj value)
        {
            return identifiers[value.GetType()];
        }
    }
}