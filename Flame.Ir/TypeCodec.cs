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
        private static readonly Symbol genericParameterSymbol = CodeSymbols.PtrArrow;

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
            else if (value is IGenericParameter)
            {
                return state.Factory.Call(
                    genericParameterSymbol,
                    state.Encode(((IGenericParameter)value).ParentMember),
                    state.Encode(value.Name));
            }

            var parent = value.Parent;
            if (parent.IsType)
            {
                var parentNode = state.Encode(parent.Type);
                var nameNode = state.Encode(value.Name);
                return state.Factory.Call(CodeSymbols.Dot, parentNode, nameNode);
            }
            else
            {
                return state.Encode(value.FullName);
            }
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
                if (AssertDecodePointerKind(data.Args[1], state, out kind))
                {
                    return elemType.MakePointerType(kind);
                }
                else
                {
                    return ErrorType.Instance;
                }
            }
            else if (data.Calls(genericParameterSymbol))
            {
                if (!FeedbackHelpers.AssertArgCount(data, 2, state.Log))
                {
                    return ErrorType.Instance;
                }

                IGenericMember parent;
                SimpleName name;

                if (state.AssertDecodeGenericMember(data.Args[0], out parent)
                    && state.AssertDecodeSimpleName(data.Args[1], out name))
                {
                    var types = state.TypeResolver.ResolveGenericParameters(parent, name);
                    if (AssertSingleChildType(types, data, state, "generic declaration"))
                    {
                        return types[0];
                    }
                }
                return ErrorType.Instance;
            }
            else if (data.Calls(CodeSymbols.Of))
            {
                if (!FeedbackHelpers.AssertMinArgCount(data, 2, state.Log))
                {
                    return ErrorType.Instance;
                }

                var genericDecl = state.DecodeType(data.Args[0]);
                var genericArgs = data.Args.Slice(1).EagerSelect<LNode, IType>(state.DecodeType);

                int count = genericDecl.GenericParameters.Count;
                if (count != genericArgs.Count)
                {
                    FeedbackHelpers.LogSyntaxError(
                        state.Log,
                        data,
                        FeedbackHelpers.QuoteEven(
                            "type ",
                            FeedbackHelpers.Print(data.Args[0]),
                            " is instantiated with ",
                            genericArgs.Count.ToString(),
                            " arguments but has only ",
                            count.ToString(),
                            " parameters."));
                    return ErrorType.Instance;
                }

                return genericDecl.MakeGenericType(genericArgs);
            }
            else if (data.Calls(CodeSymbols.Dot))
            {
                if (!FeedbackHelpers.AssertArgCount(data, 2, state.Log))
                {
                    return ErrorType.Instance;
                }

                SimpleName childName;
                if (!state.AssertDecodeSimpleName(data.Args[1], out childName))
                {
                    return ErrorType.Instance;
                }

                var parentType = state.DecodeType(data.Args[0]);
                if (parentType == ErrorType.Instance)
                {
                    // Make sure that we don't log an additional error
                    // just because the parent type was wrong.
                    return ErrorType.Instance;
                }

                var childTypes = state.TypeResolver.ResolveNestedTypes(parentType, childName);
                if (AssertSingleChildType(childTypes, data, state, "type"))
                {
                    return childTypes[0];
                }
                else
                {
                    return ErrorType.Instance;
                }
            }
            else
            {
                QualifiedName fullName;
                if (state.AssertDecodeQualifiedName(data, out fullName))
                {
                    var types = state.TypeResolver.ResolveTypes(fullName);
                    if (AssertSingleGlobalType(types, data, state))
                    {
                        return types[0];
                    }
                    else
                    {
                        return ErrorType.Instance;
                    }
                }
                else
                {
                    return ErrorType.Instance;
                }
            }
        }

        private static bool AssertSingleChildType(
            IReadOnlyList<IType> types,
            LNode parentAndNameNode,
            DecoderState state,
            string parentKindDescription)
        {
            if (types.Count == 1)
            {
                return true;
            }

            var parentNode = parentAndNameNode.Args[0];
            var nameNode = parentAndNameNode.Args[1];
            if (types.Count == 0)
            {
                FeedbackHelpers.LogSyntaxError(
                    state.Log,
                    nameNode,
                    FeedbackHelpers.QuoteEven(
                        parentKindDescription + " ",
                        FeedbackHelpers.Print(parentNode),
                        " does not define a type named ",
                        FeedbackHelpers.Print(nameNode),
                        "."));
            }
            else
            {
                FeedbackHelpers.LogSyntaxError(
                    state.Log,
                    nameNode,
                    FeedbackHelpers.QuoteEven(
                        parentKindDescription + " ",
                        FeedbackHelpers.Print(parentNode),
                        " defines more than one type named ",
                        FeedbackHelpers.Print(nameNode),
                        "."));
            }
            return false;
        }

        private static bool AssertSingleGlobalType(
            IReadOnlyList<IType> types,
            LNode nameNode,
            DecoderState state)
        {
            if (types.Count == 1)
            {
                return true;
            }

            if (types.Count == 0)
            {
                FeedbackHelpers.LogSyntaxError(
                    state.Log,
                    nameNode,
                    FeedbackHelpers.QuoteEven(
                        "there is no type named ",
                        FeedbackHelpers.Print(nameNode),
                        "."));
            }
            else
            {
                FeedbackHelpers.LogSyntaxError(
                    state.Log,
                    nameNode,
                    FeedbackHelpers.QuoteEven(
                        "there is more than one type named ",
                        FeedbackHelpers.Print(nameNode),
                        "."));
            }
            return false;
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
