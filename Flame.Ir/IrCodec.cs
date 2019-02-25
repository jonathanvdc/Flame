using System;
using System.Collections.Generic;
using Flame.Compiler;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// An encoder/decoder for every configurable element of Flame's
    /// intermediate representation.
    /// </summary>
    public struct IrCodec
    {
        /// <summary>
        /// Creates a codec for Flame IR from a number of sub-codecs.
        /// </summary>
        /// <param name="constants">A codec for constants.</param>
        /// <param name="instructionCodec">An instruction prototype codec.</param>
        /// <param name="typeCodec">A codec for type references.</param>
        /// <param name="typeMemberCodec">A codec for type member references.</param>
        /// <param name="typeMemberDefinitionCodec">A codec for type member definitions.</param>
        /// <param name="typeDefinitionCodec">A codec for method definitions.</param>
        /// <param name="attributeCodec">A codec for attributes.</param>
        public IrCodec(
            Codec<Constant, LNode> constants,
            Codec<InstructionPrototype, LNode> instructionCodec,
            Codec<IType, LNode> typeCodec,
            Codec<ITypeMember, LNode> typeMemberCodec,
            Codec<ITypeMember, LNode> typeMemberDefinitionCodec,
            Codec<IType, LNode> typeDefinitionCodec,
            Codec<IAttribute, LNode> attributeCodec)
        {
            this.Constants = constants;
            this.Instructions = instructionCodec;
            this.Types = typeCodec;
            this.TypeMembers = typeMemberCodec;
            this.TypeMemberDefinitions = typeMemberDefinitionCodec;
            this.TypeDefinitions = typeDefinitionCodec;
            this.Attributes = attributeCodec;
        }

        /// <summary>
        /// Gets the encoder for constants.
        /// </summary>
        /// <returns>The constant codec.</returns>
        public Codec<Constant, LNode> Constants { get; private set; }

        /// <summary>
        /// Gets the encoder for instruction prototypes.
        /// </summary>
        /// <returns>The instruction prototype codec.</returns>
        public Codec<InstructionPrototype, LNode> Instructions { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for type references.
        /// </summary>
        /// <returns>The type reference codec.</returns>
        public Codec<IType, LNode> Types { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for type member references.
        /// </summary>
        /// <returns>The type member reference codec.</returns>
        public Codec<ITypeMember, LNode> TypeMembers { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for type member definitions.
        /// </summary>
        /// <returns>The type member definition codec.</returns>
        public Codec<ITypeMember, LNode> TypeMemberDefinitions { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for type definitions.
        /// </summary>
        /// <returns>The type definition codec.</returns>
        public Codec<IType, LNode> TypeDefinitions { get; private set; }

        /// <summary>
        /// Gets the encoder/decoder for attributes.
        /// </summary>
        /// <returns>The attribute codec.</returns>
        public Codec<IAttribute, LNode> Attributes { get; private set; }

        /// <summary>
        /// The default codec for Flame IR as used by unmodified versions of Flame.
        /// </summary>
        public static IrCodec Default = new IrCodec(
            ConstantCodec.Instance,
            InstructionCodecElements.All,
            TypeCodec.Instance,
            TypeMemberCodec.Instance,
            TypeMemberDefinitionCodec.Instance,
            TypeDefinitionCodec.Instance,
            new PiecewiseCodec<IAttribute>());

        /// <summary>
        /// Creates an IR codec with a particular type codec.
        /// All other fields are copied from this codec.
        /// </summary>
        /// <param name="typeCodec">A type codec.</param>
        /// <returns>An IR codec.</returns>
        public IrCodec WithTypes(Codec<IType, LNode> typeCodec)
        {
            return new IrCodec(
                Constants,
                Instructions,
                typeCodec,
                TypeMembers,
                TypeMembers,
                TypeDefinitions,
                Attributes);
        }
    }
}
