using System;
using System.Collections.Generic;
using System.Numerics;
using Flame.Compiler;
using Flame.Constants;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// Decodes Loyc LNodes to Flame's intermediate representation.
    /// </summary>
    public sealed class DecoderState
    {
        /// <summary>
        /// Creates a decoder from a log and a codec.
        /// </summary>
        /// <param name="log">A log to use for error and warning messages.</param>
        /// <param name="codec">A Flame IR codec.</param>
        public DecoderState(ILog log, IrCodec codec)
        {
            this.Log = log;
            this.Codec = codec;
        }

        /// <summary>
        /// Gets a log to use for error and warning messages.
        /// </summary>
        /// <returns>A log.</returns>
        public ILog Log { get; private set; }

        /// <summary>
        /// Gets the codec used by this decoder.
        /// </summary>
        /// <returns>The codec.</returns>
        public IrCodec Codec { get; private set; }

        private T Decode<T>(LNode node, Codec<T, IReadOnlyList<LNode>> codec)
        {
            if (!FeedbackHelpers.AssertIsCall(node, Log)
                && !FeedbackHelpers.AssertIsId(node.Target, Log))
            {
                return default(T);
            }

            var identifier = node.Name;
            var args = node.Args;
            return codec.Decode(identifier, args, this);
        }

        /// <summary>
        /// Decodes an LNode as a type reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded type reference.
        /// </returns>
        public IType DecodeType(LNode node)
        {
            return Decode<IType>(node, Codec.TypeCodec);
        }

        /// <summary>
        /// Decodes an LNode as a method reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded method reference.
        /// </returns>
        public IMethod DecodeMethod(LNode node)
        {
            return Decode<IMethod>(node, Codec.MethodCodec);
        }

        /// <summary>
        /// Decodes an LNode as an instruction prototype.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded instruction prototype.
        /// </returns>
        public InstructionPrototype DecodeInstructionProtoype(LNode node)
        {
            return Decode<InstructionPrototype>(node, Codec.InstructionCodec);
        }

        /// <summary>
        /// Decodes an LNode as a constant value.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decoded constant.</returns>
        public Constant DecodeConstant(LNode node)
        {
            if (!FeedbackHelpers.AssertIsLiteral(node, Log))
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
                            Log,
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
                        Log,
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
                Log,
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
