using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
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
            else if (value is DirectTypeSpecialization)
            {
                var genericType = (DirectTypeSpecialization)value;
                var argNodes = new IType[] { genericType.Declaration }
                    .Concat<IType>(genericType.GenericArguments)
                    .Select<IType, LNode>(state.Encode);

                return state.Factory.Call(CodeSymbols.Of, argNodes);
            }

            var parent = value.Parent;
            if (parent.IsType)
            {
                var parentNode = state.Encode(parent.Type);
                var nameNode = EncodeSimpleName(value.Name, state);
                return state.Factory.Call(CodeSymbols.Dot, parentNode, nameNode);
            }
            else
            {
                return EncodeQualifiedName(value.FullName, state);
            }
        }

        private LNode EncodePointerKind(PointerKind kind, EncoderState state)
        {
            return state.Factory.Id(pointerKindEncoding[kind]);
        }

        private static LNode EncodeSimpleName(UnqualifiedName name, EncoderState state)
        {
            if (name is SimpleName)
            {
                var simple = (SimpleName)name;
                var simpleNameNode = state.Factory.Id(simple.Name);
                if (simple.TypeParameterCount == 0)
                {
                    return simpleNameNode;
                }
                else
                {
                    return state.Factory.Call(
                        simpleNameNode,
                        state.Factory.Literal(simple.TypeParameterCount));
                }
            }
            else
            {
                return state.Factory.Id(name.ToString());
            }
        }

        private static LNode EncodeQualifiedName(QualifiedName name, EncoderState state)
        {
            var accumulator = EncodeSimpleName(name.Qualifier, state);
            for (int i = 1; i < name.PathLength; i++)
            {
                accumulator = state.Factory.Call(
                    CodeSymbols.ColonColon,
                    accumulator,
                    EncodeSimpleName(name.Path[i], state));
            }
            return accumulator;
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
                if (AssertDecodePointerKind(data.Args[1], state, out kind))
                {
                    return elemType.MakePointerType(kind);
                }
                else
                {
                    return ErrorType.Instance;
                }
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