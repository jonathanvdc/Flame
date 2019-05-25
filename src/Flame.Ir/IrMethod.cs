using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Loyc.Collections;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// A method that is decoded from a Flame IR method LNode.
    /// </summary>
    public class IrMethod : IrMember, IBodyMethod
    {
        /// <summary>
        /// Creates a Flame IR method from an appropriately-encoded
        /// LNode.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="decoder">The decoder to use.</param>
        private IrMethod(LNode node, DecoderState decoder)
            : base(node, decoder)
        {
            this.isStaticCache = new Lazy<bool>(() =>
                decoder.DecodeBoolean(node.Args[1]));

            var methodDecoder = decoder.WithScope(new TypeParent(this));
            this.genericParameterCache = new Lazy<IReadOnlyList<IGenericParameter>>(() =>
                node.Args[2].Args.EagerSelect(methodDecoder.DecodeGenericParameterDefinition));
            this.returnParameterCache = new Lazy<Parameter>(() =>
                methodDecoder.DecodeParameter(node.Args[3]));
            this.parameterCache = new Lazy<IReadOnlyList<Parameter>>(() =>
                node.Args[4].Args.EagerSelect(methodDecoder.DecodeParameter));
            this.baseMethodCache = new Lazy<IReadOnlyList<IMethod>>(() =>
                node.Args[5].Args.EagerSelect(methodDecoder.DecodeMethod));
            this.methodBodyCache = new Lazy<MethodBody>(() =>
                node.ArgCount > 6
                ? new MethodBody(
                    ReturnParameter,
                    Parameter.CreateThisParameter(this.ParentType),
                    Parameters,
                    methodDecoder.DecodeFlowGraph(node.Args[6]))
                : null);
        }

        /// <summary>
        /// Decodes a method from an LNode.
        /// </summary>
        /// <param name="data">The LNode to decode.</param>
        /// <param name="state">The decoder to use.</param>
        /// <returns>
        /// A decoded method if the node can be decoded;
        /// otherwise, <c>null</c>.
        /// </returns>
        public static IMethod Decode(LNode data, DecoderState state)
        {
            SimpleName name;

            if (!FeedbackHelpers.AssertMinArgCount(data, 6, state.Log)
                || !state.AssertDecodeSimpleName(data.Args[0], out name))
            {
                return null;
            }
            else
            {
                return new IrMethod(data, state);
            }
        }

        /// <summary>
        /// Encodes a method as an LNode.
        /// </summary>
        /// <param name="value">The method to encode.</param>
        /// <param name="state">The encoder to use.</param>
        /// <returns>An LNode that represents the method.</returns>
        public static LNode Encode(IMethod value, EncoderState state)
        {
            var typeParamsNode = state.Factory.Call(
                CodeSymbols.AltList,
                value.GenericParameters
                    .Select(state.EncodeDefinition)
                    .ToList());

            var parameterNodes = state.Factory.Call(
                CodeSymbols.AltList,
                value.Parameters.EagerSelect(state.EncodeDefinition));

            var baseMethodNodes = state.Factory.Call(
                CodeSymbols.AltList,
                value.BaseMethods.EagerSelect(state.Encode));

            var argNodes = new List<LNode>()
            {
                state.Encode(value.Name),
                state.Encode(value.IsStatic),
                typeParamsNode,
                state.EncodeDefinition(value.ReturnParameter),
                parameterNodes,
                baseMethodNodes
            };

            if (value is IBodyMethod)
            {
                var body = ((IBodyMethod)value).Body;
                if (body != null)
                {
                    argNodes.Add(state.Encode(body.Implementation));
                }
            }

            return state.Factory.Call(
                value.IsConstructor ? CodeSymbols.Constructor : CodeSymbols.Fn,
                argNodes)
                .WithAttrs(
                    new VList<LNode>(
                        state.Encode(value.Attributes)));
        }

        private Lazy<IReadOnlyList<IGenericParameter>> genericParameterCache;
        private Lazy<bool> isStaticCache;
        private Lazy<Parameter> returnParameterCache;
        private Lazy<IReadOnlyList<Parameter>> parameterCache;
        private Lazy<IReadOnlyList<IMethod>> baseMethodCache;
        private Lazy<MethodBody> methodBodyCache;

        /// <inheritdoc/>
        public bool IsConstructor => Node.Calls(CodeSymbols.Constructor);

        /// <inheritdoc/>
        public bool IsStatic => isStaticCache.Value;

        /// <inheritdoc/>
        public Parameter ReturnParameter => returnParameterCache.Value;

        /// <inheritdoc/>
        public IReadOnlyList<Parameter> Parameters => parameterCache.Value;

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> BaseMethods => baseMethodCache.Value;

        /// <inheritdoc/>
        public IType ParentType => Decoder.Scope.TypeOrNull;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters => genericParameterCache.Value;

        /// <inheritdoc/>
        public MethodBody Body => methodBodyCache.Value;
    }
}
