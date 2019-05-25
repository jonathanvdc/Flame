using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// A type that is decoded from a Flame IR type LNode.
    /// </summary>
    public class IrType : IrMember, IType
    {
        /// <summary>
        /// Creates a type that is the decoded version of a node.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="decoder">The decoder to use.</param>
        internal IrType(LNode node, DecoderState decoder)
            : base(node, decoder)
        {
            var typeParamDecoder = decoder.WithScope(new TypeParent(this));
            this.genericParameterCache = new Lazy<IReadOnlyList<IGenericParameter>>(() =>
                node.Args[1].Args.EagerSelect(typeParamDecoder.DecodeGenericParameterDefinition));
            this.baseTypeCache = new Lazy<IReadOnlyList<IType>>(() =>
                node.Args[2].Args.EagerSelect(decoder.DecodeType));
            this.initializer = DeferredInitializer.Create(DecodeMembers);
        }

        private static readonly Symbol TypeDefinitionSymbol = GSymbol.Get("#type");
        private static readonly Symbol TypeParameterDefinitionSymbol = GSymbol.Get("#type_param");

        private void DecodeMembers()
        {
            this.fieldCache = new List<IField>();
            this.methodCache = new List<IMethod>();
            this.propertyCache = new List<IProperty>();
            this.nestedTypeCache = new List<IType>();

            var subDecoder = Decoder.WithScope(new TypeParent(this));
            foreach (var memberNode in Node.Args[3].Args)
            {
                if (memberNode.Calls(TypeDefinitionSymbol))
                {
                    nestedTypeCache.Add(subDecoder.DecodeTypeDefinition(memberNode));
                }
                else
                {
                    var member = subDecoder.DecodeTypeMemberDefinition(memberNode);
                    if (member is IField)
                    {
                        fieldCache.Add((IField)member);
                    }
                    else if (member is IMethod)
                    {
                        methodCache.Add((IMethod)member);
                    }
                    else
                    {
                        propertyCache.Add((IProperty)member);
                    }
                }
            }
        }

        /// <summary>
        /// Decodes an LNode as a type definition.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="state">The decoder's state.</param>
        /// <returns>A decoded type.</returns>
        public static IrType Decode(LNode node, DecoderState state)
        {
            QualifiedName name;
            if (!FeedbackHelpers.AssertArgCount(node, 4, state.Log)
                || !state.AssertDecodeQualifiedName(node.Args[0], out name))
            {
                return null;
            }
            else if (node.Calls(TypeParameterDefinitionSymbol))
            {
                return new IrGenericParameter(node, state);
            }
            else if (node.Calls(TypeDefinitionSymbol))
            {
                return new IrType(node, state);
            }
            else
            {
                state.Log.LogSyntaxError(
                    node,
                    FeedbackHelpers.QuoteEven(
                        "expected ",
                        TypeDefinitionSymbol.Name,
                        " or ",
                        TypeParameterDefinitionSymbol.Name,
                        "."));
                return null;
            }
        }

        /// <summary>
        /// Encodes a type definition as an LNode.
        /// </summary>
        /// <param name="value">The type definition to encode.</param>
        /// <param name="state">The encoder state.</param>
        /// <returns>An LNode that represents the type definition.</returns>
        public static LNode Encode(IType value, EncoderState state)
        {
            var nameNode = state.Encode(
                value.Parent.IsType || value.Parent.IsMethod
                ? value.Name.Qualify()
                : value.FullName);

            var typeParamsNode = state.Factory.Call(
                CodeSymbols.AltList,
                value.GenericParameters
                    .Select(state.EncodeDefinition)
                    .ToList());

            var baseTypesNode = state.Factory.Call(
                CodeSymbols.AltList,
                value.BaseTypes.EagerSelect(state.Encode));

            var membersNode = state.Factory.Call(
                CodeSymbols.Braces,
                value.NestedTypes
                    .Select(state.EncodeDefinition)
                    .Concat(value.Fields.Select(state.EncodeDefinition))
                    .Concat(value.Methods.Select(state.EncodeDefinition))
                    .Concat(value.Properties.Select(state.EncodeDefinition))
                    .ToArray());

            return state.Factory.Call(
                value is IGenericParameter
                    ? TypeParameterDefinitionSymbol
                    : TypeDefinitionSymbol,
                nameNode,
                typeParamsNode,
                baseTypesNode,
                membersNode)
                .WithAttrs(
                    new VList<LNode>(
                        state.Encode(value.Attributes)));
        }

        private Lazy<IReadOnlyList<IGenericParameter>> genericParameterCache;

        private Lazy<IReadOnlyList<IType>> baseTypeCache;

        private List<IField> fieldCache;

        private List<IMethod> methodCache;

        private List<IProperty> propertyCache;

        private List<IType> nestedTypeCache;

        private DeferredInitializer initializer;

        /// <inheritdoc/>
        public TypeParent Parent => Decoder.Scope;

        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes => baseTypeCache.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => genericParameterCache.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields
        {
            get
            {
                initializer.Initialize();
                return fieldCache;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods
        {
            get
            {
                initializer.Initialize();
                return methodCache;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties
        {
            get
            {
                initializer.Initialize();
                return propertyCache;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes
        {
            get
            {
                initializer.Initialize();
                return nestedTypeCache;
            }
        }
    }

    /// <summary>
    /// A type parameter that is decoded from a Flame IR type LNode.
    /// </summary>
    internal sealed class IrGenericParameter : IrType, IGenericParameter
    {
        internal IrGenericParameter(LNode node, DecoderState decoder)
            : base(node, decoder)
        { }

        /// <inheritdoc/>
        public IGenericMember ParentMember
        {
            get
            {
                if (Parent.IsType)
                {
                    return Parent.Type;
                }
                else if (Parent.IsMethod)
                {
                    return Parent.Method;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
