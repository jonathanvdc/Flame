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
        /// A codec element for alloca-array instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<AllocaArrayPrototype, IReadOnlyList<LNode>> AllocaArray =
            new CodecElement<AllocaArrayPrototype, IReadOnlyList<LNode>>(
                "alloca_array", EncodeAllocaArray, DecodeAllocaArray);

        private static AllocaArrayPrototype DecodeAllocaArray(IReadOnlyList<LNode> data, DecoderState state)
        {
            return AllocaArrayPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeAllocaArray(AllocaArrayPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for alloca instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<AllocaPrototype, IReadOnlyList<LNode>> Alloca =
            new CodecElement<AllocaPrototype, IReadOnlyList<LNode>>(
                "alloca", EncodeAlloca, DecodeAlloca);

        private static AllocaPrototype DecodeAlloca(IReadOnlyList<LNode> data, DecoderState state)
        {
            return AllocaPrototype.Create(state.DecodeType(data[0]));
        }

        private static IReadOnlyList<LNode> EncodeAlloca(AllocaPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.ResultType) };
        }

        /// <summary>
        /// A codec element for constant instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<ConstantPrototype, IReadOnlyList<LNode>> Constant =
            new CodecElement<ConstantPrototype, IReadOnlyList<LNode>>(
                "const", EncodeConstant, DecodeConstant);

        private static ConstantPrototype DecodeConstant(IReadOnlyList<LNode> data, DecoderState state)
        {
            return ConstantPrototype.Create(state.DecodeConstant(data[0]), state.DecodeType(data[1]));
        }

        private static IReadOnlyList<LNode> EncodeConstant(ConstantPrototype value, EncoderState state)
        {
            return new LNode[] { state.Encode(value.Value), state.Encode(value.ResultType) };
        }

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
                    .Add(AllocaArray)
                    .Add(Alloca)
                    .Add(Constant)
                    .Add(Copy);
            }
        }
    }
}