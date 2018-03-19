using System;
using System.Collections.Generic;
using System.Numerics;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// Decodes Loyc LNodes to Flame's intermediate representation.
    /// </summary>
    public sealed class DecoderState
    {
        /// <summary>
        /// Creates a decoder from a log and a codec.
        /// </summary>
        /// <param name="log">A log to use for error and warning messages.</param>
        /// <param name="typeResolver">A read-only type resolver for resolving types.</param>
        /// <param name="codec">A Flame IR codec.</param>
        public DecoderState(ILog log, ReadOnlyTypeResolver typeResolver, IrCodec codec)
        {
            this.Log = log;
            this.TypeResolver = typeResolver;
            this.Codec = codec;
        }

        /// <summary>
        /// Creates a decoder from a log and the default codec.
        /// </summary>
        /// <param name="log">A log to use for error and warning messages.</param>
        /// <param name="typeResolver">A read-only type resolver for resolving types.</param>
        public DecoderState(ILog log, ReadOnlyTypeResolver typeResolver)
            : this(log, typeResolver, IrCodec.Default)
        { }

        /// <summary>
        /// Gets a log to use for error and warning messages.
        /// </summary>
        /// <returns>A log.</returns>
        public ILog Log { get; private set; }

        /// <summary>
        /// Gets the codec used by this decoder.
        /// </summary>
        /// <returns>The codec.</returns>
        public IrCodec Codec { get; private set; }

        /// <summary>
        /// Gets the read-only type resolver for this decoder state.
        /// </summary>
        /// <returns>A type resolver.</returns>
        public ReadOnlyTypeResolver TypeResolver { get; private set; }

        /// <summary>
        /// Decodes an LNode as a type reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded type reference.
        /// </returns>
        public IType DecodeType(LNode node)
        {
            return Codec.Types.Decode(node, this);
        }

        /// <summary>
        /// Decodes an LNode as a method reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded method reference.
        /// </returns>
        public IMethod DecodeMethod(LNode node)
        {
            return Codec.Methods.Decode(node, this);
        }

        /// <summary>
        /// Decodes an LNode as an instruction prototype.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded instruction prototype.
        /// </returns>
        public InstructionPrototype DecodeInstructionProtoype(LNode node)
        {
            return Codec.Instructions.Decode(node, this);
        }

        private static readonly Dictionary<Symbol, MethodLookup> methodLookupDecodeMap =
            new Dictionary<Symbol, MethodLookup>()
        {
            { GSymbol.Get("static"), MethodLookup.Static },
            { GSymbol.Get("virtual"), MethodLookup.Virtual }
        };

        /// <summary>
        /// Decodes an LNode as a method lookup strategy.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A method lookup strategy.</returns>
        public MethodLookup DecodeMethodLookup(LNode node)
        {
            MethodLookup result;
            if (AssertDecodeEnum(node, methodLookupDecodeMap, "method lookup strategy", out result))
            {
                return result;
            }
            else
            {
                return MethodLookup.Static;
            }
        }

        /// <summary>
        /// Decodes an LNode as a Boolean constant.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A Boolean constant.</returns>
        public bool DecodeBoolean(LNode node)
        {
            var literal = DecodeConstant(node);
            if (literal == null)
            {
                // Couldn't decode the node, but that's been logged
                // already.
                return false;
            }
            else if (literal is BooleanConstant)
            {
                // Node parsed successfully as a Boolean literal.
                return ((BooleanConstant)literal).Value;
            }
            else
            {
                Log.LogSyntaxError(
                    node,
                    new Text("expected a Boolean literal."));
                return false;
            }
        }

        /// <summary>
        /// Decodes an LNode as a constant value.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decoded constant.</returns>
        public Constant DecodeConstant(LNode node)
        {
            return Codec.Constants.Decode(node, this);
        }

        /// <summary>
        /// Decodes an id node using a symbol-to-value mapping.
        /// An error is reported if the node cannot be decoded.
        /// </summary>
        /// <param name="node">A node to decode.</param>
        /// <param name="decodeMap">
        /// A mapping of symbols to values that is used for
        /// decoding the node.
        /// </param>
        /// <param name="enumDescription">
        /// A short description of the type of value that is being
        /// decoded, e.g., "method lookup strategy".
        /// </param>
        /// <param name="result">
        /// The decoded value, if any.
        /// </param>
        /// <returns>
        /// <c>true</c> if the node could be decoded; otherwise, <c>false</c>.
        /// </returns>
        public bool AssertDecodeEnum<T>(
            LNode node,
            IReadOnlyDictionary<Symbol, T> decodeMap,
            string enumDescription,
            out T result)
        {
            if (!node.IsId)
            {
                Log.LogSyntaxError(
                    node,
                    FeedbackHelpers.QuoteEven(
                        "expected " + enumDescription + " (",
                        FeedbackHelpers.SpellNodeKind(LNodeKind.Id),
                        " node) but got ",
                        FeedbackHelpers.SpellNodeKind(node),
                        " node."));
                result = default(T);
                return false;
            }

            if (decodeMap.TryGetValue(node.Name, out result))
            {
                return true;
            }
            else
            {
                // Create a sorted list of all admissible values.
                var sortedKeys = new List<string>();
                foreach (var item in decodeMap.Keys)
                {
                    sortedKeys.Add(item.Name);
                }
                sortedKeys.Sort();

                // Generate a lengthy message that details exactly what
                // is admissible.
                var message = new List<string>();
                message.Add("unknown " + enumDescription + " ");
                message.Add(node.Name.Name);
                message.Add("; expected ");
                for (int i = 0; i < sortedKeys.Count - 1; i++)
                {
                    message.Add(sortedKeys[i]);
                    if (i < sortedKeys.Count - 2)
                    {
                        message.Add(", ");
                    }
                    else
                    {
                        message.Add(" or ");
                    }
                }
                message.Add(sortedKeys[sortedKeys.Count - 1]);
                message.Add(".");

                Log.LogSyntaxError(
                    node,
                    FeedbackHelpers.QuoteEven(message.ToArray()));

                return false;
            }
        }
    }
}
