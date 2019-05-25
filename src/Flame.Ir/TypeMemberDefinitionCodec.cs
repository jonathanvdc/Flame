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
    /// A codec for type member definitions.
    /// </summary>
    public sealed class TypeMemberDefinitionCodec : Codec<ITypeMember, LNode>
    {
        private TypeMemberDefinitionCodec()
        { }

        /// <summary>
        /// Gets an instance of the default type member definition codec.
        /// </summary>
        /// <returns>A type member definition codec.</returns>
        public static readonly Codec<ITypeMember, LNode> Instance
            = new TypeMemberDefinitionCodec();

        /// <inheritdoc/>
        public override LNode Encode(ITypeMember value, EncoderState state)
        {
            if (value is IField)
            {
                return IrField.Encode((IField)value, state);
            }
            else
            {
                return IrMethod.Encode((IMethod)value, state);
            }
        }

        /// <inheritdoc/>
        public override ITypeMember Decode(LNode data, DecoderState state)
        {
            if (data.Calls(CodeSymbols.Fn) || data.Calls(CodeSymbols.Constructor))
            {
                return IrMethod.Decode(data, state);
            }
            else
            {
                return IrField.Decode(data, state);
            }
        }
    }
}
