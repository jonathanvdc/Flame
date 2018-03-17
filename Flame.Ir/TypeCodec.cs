using System;
using System.Collections.Generic;
using Flame.TypeSystem;
using Loyc;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// A codec for type references.
    /// </summary>
    public class TypeCodec : Codec<IType, LNode>
    {
        private TypeCodec()
        {
            this.pointerKindEncoding = new Dictionary<PointerKind, Symbol>();
            this.pointerKindDecoding = new Dictionary<Symbol, PointerKind>();

            var ptrKinds = new[]
            {
                PointerKind.Box,
                PointerKind.Reference,
                PointerKind.Transient
            };

            foreach (var item in ptrKinds)
            {
                var repr = GSymbol.Get(item.ToString());
                pointerKindEncoding.Add(item, repr);
                pointerKindDecoding.Add(repr, item);
            }
        }

        /// <summary>
        /// An instance of the type codec.
        /// </summary>
        public static readonly TypeCodec Instance = new TypeCodec();

        private static readonly Symbol pointerSymbol = GSymbol.Get("#pointer");

        private Dictionary<PointerKind, Symbol> pointerKindEncoding;
        private Dictionary<Symbol, PointerKind> pointerKindDecoding;

        /// <inheritdoc/>
        public override LNode Encode(IType value, EncoderState state)
        {
            if (value is PointerType)
            {
                var pointerType = (PointerType)value;
                var elemNode = state.Encode(pointerType.ElementType);
                var kindNode = EncodePointerKind(pointerType.Kind, state);
                return state.Factory.Call(pointerSymbol, elemNode, kindNode);
            }
            throw new NotImplementedException();
        }

        private LNode EncodePointerKind(PointerKind kind, EncoderState state)
        {
            return state.Factory.Id(pointerKindEncoding[kind]);
        }

        /// <inheritdoc/>
        public override IType Decode(LNode data, DecoderState state)
        {
            if (data.Calls(pointerSymbol))
            {
                if (!FeedbackHelpers.AssertArgCount(data, 2, state.Log))
                {
                    return ErrorType.Instance;
                }

                var elemType = state.DecodeType(data.Args[0]);
                PointerKind kind;
                if (!AssertDecodePointerKind(data.Args[1], state, out kind))
                {
                    return ErrorType.Instance;
                }

                return elemType.MakePointerType(kind);
            }
            throw new NotImplementedException();
        }

        private bool AssertDecodePointerKind(
            LNode node,
            DecoderState state,
            out PointerKind kind)
        {
            return state.AssertDecodeEnum(
                node,
                pointerKindDecoding,
                "pointer kind",
                out kind);
        }
    }
}