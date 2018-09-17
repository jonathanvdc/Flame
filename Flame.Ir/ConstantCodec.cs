using System;
using System.Numerics;
using Flame.Constants;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// A codec for constants.
    /// </summary>
    public sealed class ConstantCodec : Codec<Constant, LNode>
    {
        private ConstantCodec()
        { }

        /// <summary>
        /// An instance of the constant codec.
        /// </summary>
        public static readonly ConstantCodec Instance =
            new ConstantCodec();

        /// <summary>
        /// Encodes a constant value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="state">The encoder state to use.</param>
        /// <returns>An encoded constant value.</returns>
        public override LNode Encode(Constant value, EncoderState state)
        {
            if (value is NullConstant)
            {
                return state.Factory.Null;
            }
            else if (value is DefaultConstant)
            {
                return state.Factory.Id(CodeSymbols.Default);
            }
            else if (value is StringConstant)
            {
                return state.Factory.Literal(((StringConstant)value).Value);
            }
            else if (value is IntegerConstant)
            {
                var integerConst = (IntegerConstant)value;

                // Try to encode integer types supported natively by
                // Loyc as integer literals instead of custom literals.
                if (integerConst.Spec.Equals(IntegerSpec.UInt1))
                {
                    return state.Factory.Literal(((IntegerConstant)value).Value != 0);
                }
                else if (integerConst.Spec.Equals(IntegerSpec.UInt32))
                {
                    return state.Factory.Literal(integerConst.ToUInt32());
                }
                else if (integerConst.Spec.Equals(IntegerSpec.UInt64))
                {
                    return state.Factory.Literal(integerConst.ToUInt64());
                }
                else if (integerConst.Spec.Equals(IntegerSpec.Int32))
                {
                    return state.Factory.Literal(integerConst.ToInt32());
                }
                else if (integerConst.Spec.Equals(IntegerSpec.Int64))
                {
                    return state.Factory.Literal(integerConst.ToInt64());
                }
                else
                {
                    // Encode other integer constants as custom literals.
                    return state.Factory.Literal(
                        new CustomLiteral(
                            integerConst.Value.ToString(),
                            GSymbol.Get(integerConst.Spec.ToString())));
                }
            }
            else if (value is Float32Constant)
            {
                return state.Factory.Literal(((Float32Constant)value).Value);
            }
            else if (value is Float64Constant)
            {
                return state.Factory.Literal(((Float64Constant)value).Value);
            }

            throw new NotSupportedException(
                "Cannot encode unknown kind of literal '" + value.ToString() + "'.");
        }

        /// <summary>
        /// Decodes an LNode as a constant value.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="state">The decoder state to use.</param>
        /// <returns>A decoded constant.</returns>
        public override Constant Decode(LNode node, DecoderState state)
        {
            // Default-value constants.
            if (node.IsIdNamed(CodeSymbols.Default))
            {
                return DefaultConstant.Instance;
            }

            if (!FeedbackHelpers.AssertIsLiteral(node, state.Log))
            {
                return null;
            }

            object value;
            Symbol typeMarker;

            // Custom literals.
            if (TryDecomposeCustomLiteral(node, out value, out typeMarker))
            {
                // Arbitrary-width integer literals.
                IntegerSpec spec;
                if (IntegerSpec.TryParse(typeMarker.Name, out spec))
                {
                    BigInteger integerVal;
                    if (BigInteger.TryParse(value.ToString(), out integerVal))
                    {
                        return new IntegerConstant(integerVal, spec);
                    }
                    else
                    {
                        FeedbackHelpers.LogSyntaxError(
                            state.Log,
                            node,
                            FeedbackHelpers.QuoteEven(
                                "cannot parse ",
                                value.ToString(),
                                " as an integer."));
                        return null;
                    }
                }
                else
                {
                    FeedbackHelpers.LogSyntaxError(
                        state.Log,
                        node,
                        FeedbackHelpers.QuoteEven(
                            "unknown custom literal type ",
                            typeMarker.Name,
                            "."));
                    return null;
                }
            }

            value = node.Value;

            // Miscellaneous constants: null, strings, Booleans.
            if (value == null)
            {
                return NullConstant.Instance;
            }
            else if (value is string)
            {
                return new StringConstant((string)value);
            }
            else if (value is bool)
            {
                return BooleanConstant.Create((bool)value);
            }
            // Floating-point numbers.
            else if (value is float)
            {
                return new Float32Constant((float)value);
            }
            else if (value is double)
            {
                return new Float64Constant((double)value);
            }
            // Fixed-width integer constants and characters.
            else if (value is char)
            {
                return new IntegerConstant((char)value);
            }
            else if (value is sbyte)
            {
                return new IntegerConstant((sbyte)value);
            }
            else if (value is short)
            {
                return new IntegerConstant((short)value);
            }
            else if (value is int)
            {
                return new IntegerConstant((int)value);
            }
            else if (value is long)
            {
                return new IntegerConstant((long)value);
            }
            else if (value is byte)
            {
                return new IntegerConstant((byte)value);
            }
            else if (value is ushort)
            {
                return new IntegerConstant((ushort)value);
            }
            else if (value is uint)
            {
                return new IntegerConstant((uint)value);
            }
            else if (value is ulong)
            {
                return new IntegerConstant((ulong)value);
            }

            FeedbackHelpers.LogSyntaxError(
                state.Log,
                node,
                new Text("unknown literal type."));
            return null;
        }

        private static bool TryDecomposeCustomLiteral(
            LNode node,
            out object value,
            out Symbol typeMarker)
        {
            var val = node.Value;
            if (val is CustomLiteral)
            {
                var literal = (CustomLiteral)val;
                value = literal.Value;
                typeMarker = literal.TypeMarker;
                return true;
            }
            else
            {
                value = null;
                typeMarker = null;
                return false;
            }
        }
    }
}