using System;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// Defines codec elements for instruction prototypes.
    /// </summary>
    public static class InstructionCodecElements
    {
        /// <summary>
        /// A codec element for copy instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<CopyPrototype, IReadOnlyList<LNode>> Copy =
            new CodecElement<CopyPrototype, IReadOnlyList<LNode>>(
                "copy", EncodeCopy, DecodeCopy);

        private static CopyPrototype DecodeCopy(IReadOnlyList<LNode> data, DecoderState state)
        {
            return CopyPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeCopy(CopyPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// Gets a codec that contains all sub-codecs defined in this class.
        /// </summary>
        /// <returns>A codec.</returns>
        public static Codec<InstructionPrototype, IReadOnlyList<LNode>> All
        {
            get
            {
                return new Codec<InstructionPrototype, IReadOnlyList<LNode>>()
                    .Add(Copy);
            }
        }
    }
}