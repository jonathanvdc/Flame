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

        /// <inheritdoc/>
        public override LNode Encode(IType value, EncoderState state)
        {
            return IrType.Encode(value, state);
        }

        /// <inheritdoc/>
        public override IType Decode(LNode data, DecoderState state)
        {
            return IrType.Decode(data, state);
        }
    }
}
