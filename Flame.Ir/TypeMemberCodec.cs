using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;
using Loyc;
using Loyc.Syntax;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// A codec for type member references.
    /// </summary>
    public sealed class TypeMemberCodec : Codec<ITypeMember, LNode>
    {
        private TypeMemberCodec()
        { }

        /// <summary>
        /// A type member reference codec instance.
        /// </summary>
        public static readonly Codec<ITypeMember, LNode> Instance =
            new TypeMemberCodec();

        private readonly Symbol accessorSymbol = GSymbol.Get("#accessor");

        private readonly Dictionary<AccessorKind, string> accessorKindEncodings =
            new Dictionary<AccessorKind, string>()
        {
            { AccessorKind.Get, "get" },
            { AccessorKind.Set, "set" }
        };

        /// <inheritdoc/>
        public override ITypeMember Decode(LNode data, DecoderState state)
        {
            if (data.Calls(accessorSymbol))
            {
                if (!FeedbackHelpers.AssertArgCount(data, 2, state.Log)
                    || !FeedbackHelpers.AssertIsId(data.Args[1], state.Log))
                {
                    return null;
                }

                var property = state.DecodeProperty(data.Args[0]);
                if (property == null)
                {
                    return null;
                }
                else
                {
                    var kindName = data.Args[1].Name.Name;
                    var accessor = property.Accessors.FirstOrDefault(
                        acc => accessorKindEncodings[acc.Kind] == kindName);

                    if (accessor == null)
                    {
                        FeedbackHelpers.LogSyntaxError(
                            state.Log,
                            data.Args[1],
                            Quotation.QuoteEvenInBold(
                                "property ",
                                FeedbackHelpers.Print(data.Args[0]),
                                " does not define a ",
                                kindName,
                                " accessor."));
                    }
                    return accessor;
                }
            }
            else if (data.Calls(CodeSymbols.Dot))
            {
                // Simple dot indicates a field.
                IType parentType;
                SimpleName name;
                if (!AssertDecodeTypeAndName(data, state, out parentType, out name))
                {
                    return null;
                }

                var candidates = state.TypeMemberIndex
                    .GetAll(parentType, name)
                    .OfType<IField>()
                    .ToArray();

                return CheckSingleCandidate(
                    candidates,
                    data.Args[0],
                    data.Args[1],
                    "field",
                    state);
            }
            else if (data.CallsMin(CodeSymbols.IndexBracks, 1))
            {
                IType parentType;
                SimpleName name;
                if (!AssertDecodeTypeAndName(data.Args[0], state, out parentType, out name))
                {
                    return null;
                }

                var indexTypes = data.Args
                    .Slice(1)
                    .EagerSelect(state.DecodeType);

                var candidates = state.TypeMemberIndex
                    .GetAll(parentType, name)
                    .OfType<IProperty>()
                    .Where(prop =>
                        prop.IndexerParameters
                            .Select(p => p.Type)
                            .SequenceEqual(indexTypes))
                    .ToArray();

                return CheckSingleCandidate(
                    candidates,
                    data.Args[0].Args[0],
                    data,
                    "property",
                    state);
            }
            else if (data.Calls(CodeSymbols.Lambda))
            {
                IType parentType;
                SimpleName name;
                if (!FeedbackHelpers.AssertArgCount(data, 2, state.Log)
                    || !FeedbackHelpers.AssertIsCall(data.Args[0], state.Log)
                    || !AssertDecodeTypeAndName(data.Args[0].Target, state, out parentType, out name))
                {
                    return null;
                }

                var paramTypes = data.Args[0].Args
                    .EagerSelect(state.DecodeType);

                var retType = state.DecodeType(data.Args[1]);

                var candidates = state.TypeMemberIndex
                    .GetAll(parentType, name)
                    .OfType<IMethod>()
                    .Where(method =>
                        method.Parameters
                            .Select(p => p.Type)
                            .SequenceEqual(paramTypes)
                        && object.Equals(
                            method.ReturnParameter.Type,
                            retType))
                    .ToArray();

                return CheckSingleCandidate(
                    candidates,
                    data.Args[0].Target.Args[0],
                    data,
                    "method",
                    state);
            }
            else if (data.Calls(CodeSymbols.Of))
            {
                if (!FeedbackHelpers.AssertMinArgCount(data, 1, state.Log))
                {
                    return null;
                }

                var func = state.DecodeMethod(data.Args[0]);
                var args = data.Args.Slice(1).EagerSelect(state.DecodeType);

                if (func.GenericParameters.Count == args.Count)
                {
                    return func.MakeGenericMethod(args);
                }
                else
                {
                    state.Log.LogSyntaxError(
                        data,
                        Quotation.QuoteEvenInBold(
                            "generic arity mismatch; expected ",
                            func.GenericParameters.Count.ToString(),
                            " parameters but got ",
                            args.Count.ToString(),
                            "."));
                    return null;
                }
            }
            else
            {
                state.Log.LogSyntaxError(
                    data,
                    Quotation.QuoteEvenInBold(
                        "cannot interpret ",
                        FeedbackHelpers.Print(data),
                        " as a type member; expected a call to one of ",
                        accessorSymbol.Name, ", ",
                        CodeSymbols.Dot.Name, ", ",
                        CodeSymbols.IndexBracks.Name, ", ",
                        CodeSymbols.Of.Name, " or ",
                        CodeSymbols.Lambda.Name));
                return null;
            }
        }

        /// <inheritdoc/>
        public override LNode Encode(ITypeMember value, EncoderState state)
        {
            if (value is IAccessor)
            {
                var acc = (IAccessor)value;

                return state.Factory.Call(
                    accessorSymbol,
                    state.Encode(acc.ParentProperty),
                    state.Factory.Id(accessorKindEncodings[acc.Kind]));
            }
            else if (value is IField)
            {
                return EncodeTypeAndName(value, state);
            }
            else if (value is IProperty)
            {
                var property = (IProperty)value;

                return state.Factory.Call(
                    CodeSymbols.IndexBracks,
                    new[]
                    {
                        EncodeTypeAndName(property, state)
                    }.Concat(
                        property.IndexerParameters.EagerSelect(
                            p => state.Encode(p.Type))));
            }
            else if (value is DirectMethodSpecialization)
            {
                var spec = (DirectMethodSpecialization)value;

                return state.Factory.Call(
                    CodeSymbols.Of,
                    new[]
                    {
                        state.Encode(spec.Declaration)
                    }.Concat(
                        spec.GenericArguments.EagerSelect(state.Encode)));
            }
            else if (value is IMethod)
            {
                var method = (IMethod)value;

                return state.Factory.Call(
                    CodeSymbols.Lambda,
                    state.Factory.Call(
                        EncodeTypeAndName(method, state),
                        method.Parameters.EagerSelect(
                            p => state.Encode(p.Type))),
                    state.Encode(method.ReturnParameter.Type));
            }
            else
            {
                throw new NotSupportedException(
                    "Unknown kind of type member '" + value + "'.");
            }
        }

        private static bool AssertDecodeTypeAndName(
            LNode parentAndName,
            DecoderState state,
            out IType parentType,
            out SimpleName name)
        {
            if (!FeedbackHelpers.AssertArgCount(parentAndName, 2, state.Log))
            {
                parentType = null;
                name = null;
                return false;
            }

            parentType = state.DecodeType(parentAndName.Args[0]);

            if (parentType != ErrorType.Instance
                && state.AssertDecodeSimpleName(parentAndName.Args[1], out name))
            {
                return true;
            }
            else
            {
                parentType = null;
                name = null;
                return false;
            }
        }

        private static T CheckSingleCandidate<T>(
            T[] candidates,
            LNode parentType,
            LNode signature,
            string memberKind,
            DecoderState state)
            where T : class
        {
            if (candidates.Length == 0)
            {
                state.Log.LogSyntaxError(
                    signature,
                    Quotation.QuoteEvenInBold(
                        "type ",
                        FeedbackHelpers.Print(parentType),
                        " does not define a " + memberKind + " ",
                        FeedbackHelpers.Print(signature),
                        "."));
                return null;
            }
            else if (candidates.Length > 1)
            {
                state.Log.LogSyntaxError(
                    signature,
                    Quotation.QuoteEvenInBold(
                        "type ",
                        FeedbackHelpers.Print(parentType),
                        " defines more than one " + memberKind + " ",
                        FeedbackHelpers.Print(signature),
                        "."));
                return null;
            }
            else
            {
                return candidates[0];
            }
        }

        private static LNode EncodeTypeAndName(ITypeMember member, EncoderState state)
        {
            return state.Factory.Call(
                CodeSymbols.Dot,
                state.Encode(member.ParentType),
                state.Encode(member.Name));
        }
    }
}