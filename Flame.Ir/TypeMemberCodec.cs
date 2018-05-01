using System.Collections.Generic;
using System.Linq;
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

            // TODO: handle methods, fields and properties.

            throw new System.NotImplementedException();
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

            // TODO: handle methods, fields and properties.

            throw new System.NotImplementedException();
        }
    }
}