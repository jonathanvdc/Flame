using System;
using System.Collections.Generic;
using System.Numerics;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.Constants;
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
        /// <param name="codec">A Flame IR codec.</param>
        public DecoderState(ILog log, IrCodec codec)
        {
            this.Log = log;
            this.Codec = codec;
        }

        /// <summary>
        /// Creates a decoder from a log and the default codec.
        /// </summary>
        /// <param name="log">A log to use for error and warning messages.</param>
        public DecoderState(ILog log)
            : this(log, IrCodec.Default)
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

        /// <summary>
        /// Decodes an LNode as a method lookup strategy.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A method lookup strategy.</returns>
        public MethodLookup DecodeMethodLookup(LNode node)
        {
            if (node.IsIdNamed("virtual"))
            {
                return MethodLookup.Virtual;
            }
            else
            {
                if (!node.IsIdNamed("static"))
                {
                    Log.LogSyntaxError(
                        node,
                        FeedbackHelpers.QuoteEven(
                            "unknown method lookup strategy ",
                            node.Name.Name,
                            ". Expected either ",
                            "static",
                            " or ",
                            "virtual",
                            "."));
                }

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
    }
}
