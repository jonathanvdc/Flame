using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;
using Loyc.Syntax;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// Defines codec elements for type members.
    /// </summary>
    public static class TypeMemberCodecElements
    {
        private const string FieldIdentifier = "field";

        /// <summary>
        /// A codec element for alloca-array instruction prototypes.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<IField, LNode> Field =
            new CodecElement<IField, LNode>(
                FieldIdentifier, EncodeField, DecodeField);

        private static IField DecodeField(LNode data, DecoderState state)
        {
            SimpleName name;

            if (!FeedbackHelpers.AssertArgCount(data, 3, state.Log)
                || !state.AssertDecodeSimpleName(data.Args[0], out name))
            {
                return null;
            }

            return new DescribedField(
                state.DefiningType,
                name,
                state.DecodeBoolean(data.Args[1]),
                state.DecodeType(data.Args[2]));
        }

        private static LNode EncodeField(IField value, EncoderState state)
        {
            return state.Factory.Call(
                FieldIdentifier,
                state.Encode(value.Name),
                state.Encode(value.IsStatic),
                state.Encode(value.FieldType));
        }

        /// <summary>
        /// Gets a codec that contains all sub-codecs defined in this class.
        /// </summary>
        /// <returns>A codec.</returns>
        public static Codec<ITypeMember, LNode> All
        {
            get
            {
                return new PiecewiseCodec<ITypeMember>()
                    .Add(Field);
            }
        }
    }
}
