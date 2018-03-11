using System;
using System.Collections.Generic;
using Flame.Compiler;
using Loyc.Syntax;
using Pixie;

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

        /// <summary>
        /// Decodes an LNode as an instruction prototype.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded instruction prototype.
        /// </returns>
        public InstructionPrototype DecodeInstructionProtoype(LNode node)
        {
            if (!FeedbackHelpers.AssertIsCall(node, Log)
                && !FeedbackHelpers.AssertIsId(node.Target, Log))
            {
                return null;
            }

            var identifier = node.Name;
            var args = node.Args;
            return Codec.InstructionCodec.Decode(identifier, args, this);
        }
    }
}