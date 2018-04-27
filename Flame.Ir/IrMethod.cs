using System;
using System.Collections.Generic;
using Flame.Collections;
using Loyc;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// A method that is decoded from a Flame IR method LNode.
    /// </summary>
    public class IrMethod : IrMember, IMethod
    {
        /// <summary>
        /// Creates a Flame IR method from an appropriately-encoded
        /// LNode.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="decoder">The decoder to use.</param>
        public IrMethod(LNode node, DecoderState decoder)
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
        }

        private Lazy<IReadOnlyList<IGenericParameter>> genericParameterCache;
        private Lazy<bool> isStaticCache;
        private Lazy<Parameter> returnParameterCache;
        private Lazy<IReadOnlyList<Parameter>> parameterCache;
        private Lazy<IReadOnlyList<IMethod>> baseMethodCache;

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
    }
}
