using System;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Constants;
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
        /// Gets a log to use for error and warning messages.
        /// </summary>
        /// <returns>A log.</returns>
        public ILog Log { get; private set; }

        /// <summary>
        /// Gets the codec used by this decoder.
        /// </summary>
        /// <returns>The codec.</returns>
        public IrCodec Codec { get; private set; }

        private T Decode<T>(LNode node, Codec<T, IReadOnlyList<LNode>> codec)
        {
            if (!FeedbackHelpers.AssertIsCall(node, Log)
                && !FeedbackHelpers.AssertIsId(node.Target, Log))
            {
                return default(T);
            }

            var identifier = node.Name;
            var args = node.Args;
            return codec.Decode(identifier, args, this);
        }

        /// <summary>
        /// Decodes an LNode as a type.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded type.
        /// </returns>
        public IType DecodeType(LNode node)
        {
            return Decode<IType>(node, Codec.TypeCodec);
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
            return Decode<InstructionPrototype>(node, Codec.InstructionCodec);
        }

        /// <summary>
        /// Decodes an LNode as a constant value.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decoded constant.</returns>
        public Constant DecodeConstant(LNode node)
        {
            if (!FeedbackHelpers.AssertIsLiteral(node, Log))
            {
                return null;
            }

            var value = node.Value;

            // Miscellaneous constants: null, strings, Booleans.
            if (value == null)
            {
                return NullConstant.Instance;
            }
            else if (value is string)
            {
                return new StringConstant((string)value);
            }
            else if (value is bool)
            {
                return BooleanConstant.Create((bool)value);
            }

            // Fixed-width integer constants and characters.
            if (value is char)
            {
                return new IntegerConstant((char)value);
            }
            else if (value is sbyte)
            {
                return new IntegerConstant((sbyte)value);
            }
            else if (value is short)
            {
                return new IntegerConstant((short)value);
            }
            else if (value is int)
            {
                return new IntegerConstant((int)value);
            }
            else if (value is long)
            {
                return new IntegerConstant((long)value);
            }
            else if (value is byte)
            {
                return new IntegerConstant((byte)value);
            }
            else if (value is ushort)
            {
                return new IntegerConstant((ushort)value);
            }
            else if (value is uint)
            {
                return new IntegerConstant((uint)value);
            }
            else if (value is ulong)
            {
                return new IntegerConstant((ulong)value);
            }

            // TODO: support arbitrary-width integer constants.

            FeedbackHelpers.LogSyntaxError(
                Log,
                node,
                new Text("unknown literal type."));
            return null;
        }
    }
}
