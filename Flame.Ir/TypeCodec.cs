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
                if (!AssertDecodeSimpleName(data.Args[1], state, out childName))
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
                if (AssertSingleType(childTypes, data, state, "type"))
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
                if (AssertDecodeQualifiedName(data, state, out fullName))
                {
                    var types = state.TypeResolver.ResolveTypes(fullName);
                    if (AssertSingleType(types, data, state, "namespace"))
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

        private static bool AssertSingleType(
            IReadOnlyList<IType> types,
            LNode binaryNode,
            DecoderState state,
            string parentKindDescription)
        {
            if (types.Count == 1)
            {
                return true;
            }

            var parentNode = binaryNode.Args[0];
            var nameNode = binaryNode.Args[1];
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

        private static bool AssertDecodeSimpleName(LNode node, DecoderState state, out SimpleName name)
        {
            if (node.IsId)
            {
                name = new SimpleName(node.Name.Name);
                return true;
            }
            else if (node.IsCall)
            {
                var nameNode = node.Target;
                int arity;
                if (!FeedbackHelpers.AssertIsId(nameNode, state.Log)
                    || !FeedbackHelpers.AssertArgCount(node, 1, state.Log)
                    || !state.AssertDecodeInt32(node.Args[0], out arity))
                {
                    name = null;
                    return false;
                }

                name = new SimpleName(nameNode.Name.Name, arity);
                return true;
            }
            else
            {
                FeedbackHelpers.LogSyntaxError(
                    state.Log,
                    node,
                    FeedbackHelpers.QuoteEven(
                        "expected a simple name, which can either be a simple id (e.g., ",
                        "Type",
                        ") or a call to an id that specifies the number of generic parameters (e.g., ",
                        "Type(2)",
                        ")."));
                name = null;
                return false;
            }
        }

        private static bool AssertDecodeQualifiedName(
            LNode node,
            DecoderState state,
            out QualifiedName name)
        {
            if (node.Calls(CodeSymbols.ColonColon))
            {
                QualifiedName prefix;
                SimpleName suffix;
                if (FeedbackHelpers.AssertArgCount(node, 2, state.Log)
                    && AssertDecodeQualifiedName(node.Args[0], state, out prefix)
                    && AssertDecodeSimpleName(node.Args[1], state, out suffix))
                {
                    name = suffix.Qualify(prefix);
                    return true;
                }
                else
                {
                    name = default(QualifiedName);
                    return false;
                }
            }
            else
            {
                SimpleName simple;
                if (AssertDecodeSimpleName(node, state, out simple))
                {
                    name = simple.Qualify();
                    return true;
                }
                else
                {
                    name = default(QualifiedName);
                    return false;
                }
            }
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
