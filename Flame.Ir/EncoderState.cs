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
        /// Instantiates a Flame IR encoder.
        /// </summary>
        /// <param name="codec">The codec to use for encoding.</param>
        public EncoderState(IrCodec codec)
            : this(
                codec,
                new LNodeFactory(EmptySourceFile.Default))
        { }

        /// <summary>
        /// Instantiates a Flame IR encoder.
        /// </summary>
        public EncoderState()
            : this(IrCodec.Default)
        { }

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

        internal static readonly Symbol typeHintSymbol = GSymbol.Get("#type");
        internal static readonly Symbol methodHintSymbol = GSymbol.Get("#method");

        /// <summary>
        /// Encodes a type reference.
        /// </summary>
        /// <param name="type">The type reference to encode.</param>
        /// <returns>
        /// An encoded type reference.
        /// </returns>
        public LNode Encode(IType type)
        {
            return Codec.Types.Encode(type, this);
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
            return Codec.Methods.Encode(method, this);
        }

        public LNode Encode(IGenericMember genericMember)
        {
            if (genericMember is IType)
            {
                return Factory.Call(typeHintSymbol, Encode((IType)genericMember));
            }
            else
            {
                return Factory.Call(methodHintSymbol, Encode((IMethod)genericMember));
            }
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
            return Codec.Instructions.Encode(prototype, this);
        }

        /// <summary>
        /// Encodes a constant value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>An encoded constant value.</returns>
        public LNode Encode(Constant value)
        {
            return Codec.Constants.Encode(value, this);
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

        /// <summary>
        /// Encodes an unqualified name as a simple name.
        /// </summary>
        /// <param name="name">
        /// An unqualified name to encode. Simple names are encoded
        /// as such. Other names are encoded as simple names by taking
        /// their string representation.
        /// </param>
        /// <returns>An encoded node.</returns>
        public LNode Encode(UnqualifiedName name)
        {
            if (name is SimpleName)
            {
                var simple = (SimpleName)name;
                var simpleNameNode = Factory.Id(simple.Name);
                if (simple.TypeParameterCount == 0)
                {
                    return simpleNameNode;
                }
                else
                {
                    return Factory.Call(
                        simpleNameNode,
                        Factory.Literal(simple.TypeParameterCount));
                }
            }
            else
            {
                return Factory.Id(name.ToString());
            }
        }

        /// <summary>
        /// Encodes a qualified name as a sequence of simple names.
        /// </summary>
        /// <param name="name">
        /// A qualified name to encode. Simple names in the qualified
        /// name are encoded as such. Other names are encoded as simple
        /// names by taking their string representation.
        /// </param>
        /// <returns>An encoded node.</returns>
        public LNode Encode(QualifiedName name)
        {
            var accumulator = Encode(name.Qualifier);
            for (int i = 1; i < name.PathLength; i++)
            {
                accumulator = Factory.Call(
                    CodeSymbols.ColonColon,
                    accumulator,
                    Encode(name.Path[i]));
            }
            return accumulator;
        }
    }
}
