using System;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;

namespace Flame.Ir
{
    /// <summary>
    /// Encodes Flame's intermediate representation as Loyc LNodes.
    /// </summary>
    public sealed class EncoderState
    {
        /// <summary>
        /// Instantiates a Flame IR encoder.
        /// </summary>
        /// <param name="codec">The codec to use for encoding.</param>
        /// <param name="factory">The node factory to use for creating nodes.</param>
        public EncoderState(IrCodec codec, LNodeFactory factory)
        {
            this.Codec = codec;
            this.Factory = factory;
        }

        /// <summary>
        /// Gets the codec used by this encoder.
        /// </summary>
        /// <returns>A Flame IR codec.</returns>
        public IrCodec Codec { get; private set; }

        /// <summary>
        /// Gets the node factory for this encoder.
        /// </summary>
        /// <returns>A node factory.</returns>
        public LNodeFactory Factory { get; private set; }

        private LNode Encode<T>(T value, Codec<T, IReadOnlyList<LNode>> codec)
        {
            Symbol identifier;
            var args = codec.Encode(value, this, out identifier);
            return Factory.Call(Factory.Id(identifier), args);
        }

        /// <summary>
        /// Encodes a type reference.
        /// </summary>
        /// <param name="type">The type reference to encode.</param>
        /// <returns>
        /// An encoded type reference.
        /// </returns>
        public LNode Encode(IType type)
        {
            return Encode<IType>(type, Codec.TypeCodec);
        }

        /// <summary>
        /// Encodes a method reference.
        /// </summary>
        /// <param name="method">The method reference to encode.</param>
        /// <returns>
        /// An encoded method reference.
        /// </returns>
        public LNode Encode(IMethod method)
        {
            return Encode<IMethod>(method, Codec.MethodCodec);
        }

        /// <summary>
        /// Encodes an instruction prototype.
        /// </summary>
        /// <param name="prototype">The instruction prototype to encode.</param>
        /// <returns>
        /// An encoded instruction prototype.
        /// </returns>
        public LNode Encode(InstructionPrototype prototype)
        {
            return Encode<InstructionPrototype>(prototype, Codec.InstructionCodec);
        }

        /// <summary>
        /// Encodes a constant value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>An encoded constant value.</returns>
        public LNode Encode(Constant value)
        {
            if (value is NullConstant)
            {
                return Factory.Null;
            }
            else if (value is StringConstant)
            {
                return Factory.Literal(((StringConstant)value).Value);
            }
            else if (value is BooleanConstant)
            {
                return Factory.Literal(((BooleanConstant)value).Value);
            }
            else if (value is IntegerConstant)
            {
                var integerConst = (IntegerConstant)value;
                return Factory.Literal(
                    new CustomLiteral(
                        integerConst.Value.ToString(),
                        GSymbol.Get(integerConst.Spec.ToString())));
            }
            else if (value is Float32Constant)
            {
                return Factory.Literal(((Float32Constant)value).Value);
            }
            else if (value is Float64Constant)
            {
                return Factory.Literal(((Float64Constant)value).Value);
            }

            throw new NotSupportedException(
                "Cannot encode unknown kind of literal '" + value.ToString() + "'.");
        }

        /// <summary>
        /// Encodes a method lookup strategy as an LNode.
        /// </summary>
        /// <param name="lookup">A method lookup strategy.</param>
        /// <returns>
        /// An LNode that represents <paramref name="lookup"/>.
        /// </returns>
        public LNode Encode(MethodLookup lookup)
        {
            switch (lookup)
            {
                case MethodLookup.Static:
                    return Factory.Id("static");
                case MethodLookup.Virtual:
                    return Factory.Id("virtual");
                default:
                    throw new NotSupportedException(
                        "Cannot encode unknown method lookup type '" + lookup.ToString() + "'.");
            }
        }

        /// <summary>
        /// Encodes a Boolean constant.
        /// </summary>
        /// <param name="value">A Boolean constant to encode.</param>
        /// <returns>
        /// The encoded Boolean constant.
        /// </returns>
        public LNode Encode(bool value)
        {
            return Encode(BooleanConstant.Create(value));
        }
    }
}
