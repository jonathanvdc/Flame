using System;
using Flame.Ir;
using Loyc.Collections;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// A field that is decoded from a Flame IR field LNode.
    /// </summary>
    public sealed class IrField : IrMember, IField
    {
        /// <summary>
        /// Creates a Flame IR field from an appropriately-encoded
        /// LNode.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="decoder">The decoder to use.</param>
        private IrField(LNode node, DecoderState decoder)
            : base(node, decoder)
        {
            this.isStaticCache = new Lazy<bool>(() =>
                decoder.DecodeBoolean(node.Args[1]));
            this.fieldTypeCache = new Lazy<IType>(() =>
                decoder.DecodeType(node.Args[2]));
        }

        /// <summary>
        /// Decodes a field from an LNode.
        /// </summary>
        /// <param name="data">The LNode to decode.</param>
        /// <param name="state">The decoder to use.</param>
        /// <returns>
        /// A decoded field if the node can be decoded;
        /// otherwise, <c>null</c>.
        /// </returns>
        public static IField Decode(LNode data, DecoderState state)
        {
            SimpleName name;

            if (!FeedbackHelpers.AssertArgCount(data, 3, state.Log)
                || !state.AssertDecodeSimpleName(data.Args[0], out name))
            {
                return null;
            }
            else
            {
                return new IrField(data, state);
            }
        }

        /// <summary>
        /// Encodes a field as an LNode.
        /// </summary>
        /// <param name="value">The field to encode.</param>
        /// <param name="state">The encoder to use.</param>
        /// <returns>An LNode that represents the field.</returns>
        public static LNode Encode(IField value, EncoderState state)
        {
            return state.Factory.Call(
                CodeSymbols.Var,
                state.Encode(value.Name),
                state.Encode(value.IsStatic),
                state.Encode(value.FieldType))
                .WithAttrs(
                    new VList<LNode>(
                        state.Encode(value.Attributes)));
        }

        /// <inheritdoc/>
        public IType ParentType => Decoder.DefiningType;

        private Lazy<bool> isStaticCache;
        private Lazy<IType> fieldTypeCache;

        /// <inheritdoc/>
        public bool IsStatic => isStaticCache.Value;

        /// <inheritdoc/>
        public IType FieldType => fieldTypeCache.Value;
    }
}
