using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// A codec for type references.
    /// </summary>
    public class TypeCodec : Codec<IType, LNode>
    {
        /// <inheritdoc/>
        public override IType Decode(LNode data, DecoderState state)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public override LNode Encode(IType value, EncoderState state)
        {
            throw new System.NotImplementedException();
        }
    }
}