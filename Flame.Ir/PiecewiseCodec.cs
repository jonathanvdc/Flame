using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Flame.Collections;
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
                ImmutableDictionary<Type, Func<TObj, EncoderState, LNode>>.Empty)
        { }

        private PiecewiseCodec(
            ImmutableDictionary<Symbol, Func<LNode, DecoderState, TObj>> decoders,
            ImmutableDictionary<Type, Func<TObj, EncoderState, LNode>> encoders)
        {
            this.decoders = decoders;
            this.encoders = encoders;
            this.specializedEncoders = new Dictionary<Type, Func<TObj, EncoderState, LNode>>();
            this.specializedEncoderLock = new ReaderWriterLockSlim();
        }

        private ImmutableDictionary<Symbol, Func<LNode, DecoderState, TObj>> decoders;

        private ImmutableDictionary<Type, Func<TObj, EncoderState, LNode>> encoders;

        private Dictionary<Type, Func<TObj, EncoderState, LNode>> specializedEncoders;

        private ReaderWriterLockSlim specializedEncoderLock;

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

            return new PiecewiseCodec<TObj>(newDecoders, newEncoders);
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

            return new PiecewiseCodec<TObj>(newDecoders, newEncoders);
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
            var valType = value.GetType();
            Func<TObj, EncoderState, LNode> encoder;
            try
            {
                specializedEncoderLock.EnterReadLock();
                if (!specializedEncoders.TryGetValue(valType, out encoder))
                {
                    encoder = null;
                }
            }
            finally
            {
                specializedEncoderLock.ExitReadLock();
            }

            if (encoder == null)
            {
                try
                {
                    specializedEncoderLock.EnterWriteLock();
                    Type encoderKey;

                    if (encoders.Keys.TryGetBestElement(
                        (t1, t2) => PickMostDerivedParent(t1, t2, valType),
                        out encoderKey)
                        && encoderKey != null
                        && encoderKey.IsAssignableFrom(valType))
                    {
                        encoder = encoders[encoderKey];
                        specializedEncoders[valType] = encoder;
                    }
                }
                finally
                {
                    specializedEncoderLock.ExitWriteLock();
                }
            }

            return encoder(value, state);
        }

        private static Betterness PickMostDerivedParent(Type parent1, Type parent2, Type child)
        {
            bool t1Works = parent1.IsAssignableFrom(child);
            bool t2Works = parent2.IsAssignableFrom(child);
            if (t1Works && t2Works)
            {
                if (parent1.IsAssignableFrom(parent2))
                {
                    return Betterness.Second;
                }
                else if (parent2.IsAssignableFrom(parent1))
                {
                    return Betterness.First;
                }
                else
                {
                    return Betterness.Neither;
                }
            }
            else if (t1Works)
            {
                return Betterness.First;
            }
            else if (t2Works)
            {
                return Betterness.Second;
            }
            else
            {
                return Betterness.Neither;
            }
        }
    }
}
