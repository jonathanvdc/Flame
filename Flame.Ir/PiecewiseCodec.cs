using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Loyc;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// An encoder/decoder for a broad class of values that uses identifiers
    /// to differentiate between different types of encoded objects.
    /// </summary>
    public sealed class PiecewiseCodec<TObj> : Codec<TObj, LNode>
    {
        /// <summary>
        /// Creates an empty codec.
        /// </summary>
        public PiecewiseCodec()
            : this(
                ImmutableDictionary<Symbol, Func<LNode, DecoderState, TObj>>.Empty,
                ImmutableDictionary<Type, Func<TObj, EncoderState, LNode>>.Empty,
                ImmutableDictionary<Type, Symbol>.Empty)
        { }

        private PiecewiseCodec(
            ImmutableDictionary<Symbol, Func<LNode, DecoderState, TObj>> decoders,
            ImmutableDictionary<Type, Func<TObj, EncoderState, LNode>> encoders,
            ImmutableDictionary<Type, Symbol> identifiers)
        {
            this.decoders = decoders;
            this.encoders = encoders;
            this.identifiers = identifiers;
        }

        private ImmutableDictionary<Symbol, Func<LNode, DecoderState, TObj>> decoders;

        private ImmutableDictionary<Type, Func<TObj, EncoderState, LNode>> encoders;

        private ImmutableDictionary<Type, Symbol> identifiers;

        /// <summary>
        /// Adds an encoder for a specific type of element to this more
        /// general codec.
        /// </summary>
        /// <param name="element">A codec for a particular type of element.</param>
        /// <returns>
        /// A new codec that can encode and decode elements of type <typeparamref name="T"/>.
        /// </returns>
        public PiecewiseCodec<TObj> Add<T>(CodecElement<T, LNode> element)
            where T : TObj
        {
            var newDecoders = decoders.Add(
                element.Identifier,
                (data, state) => element.Decode(data, state));

            var newEncoders = encoders.Add(
                typeof(T),
                (obj, state) => element.Encode((T)obj, state));

            var newIdentifiers = identifiers.Add(typeof(T), element.Identifier);

            return new PiecewiseCodec<TObj>(newDecoders, newEncoders, newIdentifiers);
        }

        /// <summary>
        /// Adds an encoder for a specific type of element to this more
        /// general codec.
        /// </summary>
        /// <param name="element">A codec for a particular type of element.</param>
        /// <returns>
        /// A new codec that can encode and decode elements of type <typeparamref name="T"/>.
        /// </returns>
        public PiecewiseCodec<TObj> Add<T>(CodecElement<T, IReadOnlyList<LNode>> element)
            where T : TObj
        {
            var newDecoders = decoders.Add(
                element.Identifier,
                (data, state) => element.Decode(data.Args, state));

            var newEncoders = encoders.Add(
                typeof(T),
                (obj, state) =>
                    state.Factory.Call(
                        element.Identifier,
                        element.Encode((T)obj, state)));

            var newIdentifiers = identifiers.Add(typeof(T), element.Identifier);

            return new PiecewiseCodec<TObj>(newDecoders, newEncoders, newIdentifiers);
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
        private TObj Decode(Symbol identifier, LNode data, DecoderState state)
        {
            return decoders[identifier](data, state);
        }

        /// <summary>
        /// Decodes a particular piece of data.
        /// </summary>
        /// <param name="data">
        /// Encoded data to decode.
        /// </param>
        /// <param name="state">
        /// The decoder's state.
        /// </param>
        /// <returns>
        /// A decoded object.
        /// </returns>
        public override TObj Decode(LNode data, DecoderState state)
        {
            if (!FeedbackHelpers.AssertIsCall(data, state.Log)
                && !FeedbackHelpers.AssertIsId(data.Target, state.Log))
            {
                return default(TObj);
            }

            var identifier = data.Name;
            var args = data.Args;
            return Decode(identifier, data, state);
        }

        /// <summary>
        /// Encodes a value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="state">The state of the encoder.</param>
        /// <returns>The encoded value.</returns>
        public override LNode Encode(TObj value, EncoderState state)
        {
            return encoders[value.GetType()](value, state);
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