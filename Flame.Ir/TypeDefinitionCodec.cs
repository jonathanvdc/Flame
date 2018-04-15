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
    /// A codec for type definitions.
    /// </summary>
    public sealed class TypeDefinitionCodec : Codec<IType, LNode>
    {
        private TypeDefinitionCodec()
        { }

        /// <summary>
        /// Gets an instance of the default type definition codec.
        /// </summary>
        /// <returns>A type definition codec.</returns>
        public static readonly Codec<IType, LNode> Instance
            = new TypeDefinitionCodec();

        private static readonly Symbol TypeDefinitionSymbol = GSymbol.Get("#type");
        private static readonly Symbol TypeParameterDefinitionSymbol = GSymbol.Get("#type_param");

        /// <inheritdoc/>
        public override LNode Encode(IType value, EncoderState state)
        {
            var nameNode = state.Encode(value.Name);
            var typeParamNodes = value.GenericParameters
                .Select(state.EncodeDefinition)
                .ToList();

            if (typeParamNodes.Count > 0)
            {
                typeParamNodes.Insert(0, nameNode);
                nameNode = state.Factory.Call(CodeSymbols.Of, typeParamNodes);
            }

            var baseTypesNode = state.Factory.Call(
                CodeSymbols.Tuple,
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
                baseTypesNode,
                membersNode);
        }

        /// <inheritdoc/>
        public override IType Decode(LNode data, DecoderState state)
        {
            throw new NotImplementedException();
        }
    }
}
