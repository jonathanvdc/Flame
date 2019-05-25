using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Loyc.Collections;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// An assembly that is decoded from a Flame IR field LNode.
    /// </summary>
    public sealed class IrAssembly : IrMember, IAssembly
    {
        /// <summary>
        /// Creates a Flame IR assembly from an appropriately-encoded
        /// LNode.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="decoder">The decoder to use.</param>
        public IrAssembly(LNode node, DecoderState decoder)
            : base(node, decoder)
        {
            this.typeInitializer = DeferredInitializer.Create(() => {
                var newResolver = decoder.TypeResolver.CreateMutableCopy();
                var newDecoder = new DecoderState(
                    decoder.Log,
                    newResolver.ReadOnlyView,
                    decoder.Codec)
                    .WithScope(new TypeParent(this));

                this.typeCache = this.Node.Args[1].Args.EagerSelect(newDecoder.DecodeTypeDefinition);
                newResolver.AddAssembly(this);
            });
        }

        /// <summary>
        /// Decodes an assembly from an LNode.
        /// </summary>
        /// <param name="data">The LNode to decode.</param>
        /// <param name="state">The decoder to use.</param>
        /// <returns>
        /// A decoded assembly if the node can be decoded;
        /// otherwise, <c>null</c>.
        /// </returns>
        public static IrAssembly Decode(LNode data, DecoderState state)
        {
            QualifiedName name;
            if (!FeedbackHelpers.AssertArgCount(data, 2, state.Log)
                || !state.AssertDecodeQualifiedName(data.Args[0], out name))
            {
                return null;
            }
            else
            {
                return new IrAssembly(data, state);
            }
        }

        /// <summary>
        /// Encodes an assembly as an LNode.
        /// </summary>
        /// <param name="value">The assembly to encode.</param>
        /// <param name="state">The encoder to use.</param>
        /// <returns>An LNode that represents the assembly.</returns>
        public static LNode Encode(IAssembly value, EncoderState state)
        {
            return state.Factory.Call(
                CodeSymbols.Assembly,
                state.Encode(value.FullName),
                state.Factory.Call(
                    CodeSymbols.Braces,
                    value.Types.EagerSelect(state.EncodeDefinition)))
                .WithAttrs(
                    new VList<LNode>(
                        state.Encode(value.Attributes)));
        }

        private IReadOnlyList<IType> typeCache;
        private DeferredInitializer typeInitializer;

        /// <inheritdoc/>
        public IReadOnlyList<IType> Types
        {
            get
            {
                typeInitializer.Initialize();
                return this.typeCache;
            }
        }
    }
}
