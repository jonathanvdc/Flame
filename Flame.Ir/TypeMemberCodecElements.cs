using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;
using Loyc.Syntax;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// Defines codec elements for type members.
    /// </summary>
    public static class TypeMemberCodecElements
    {
        /// <summary>
        /// A codec element for fields.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<IField, LNode> Field =
            new CodecElement<IField, LNode>(
                CodeSymbols.Var, IrField.Encode, IrField.Decode);

        /// <summary>
        /// A codec element for constructors.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<IMethod, LNode> Constructor =
            new CodecElement<IMethod, LNode>(
                CodeSymbols.Constructor, IrMethod.Encode, IrMethod.Decode);

        /// <summary>
        /// A codec element for methods.
        /// </summary>
        /// <returns>A codec element.</returns>
        public static readonly CodecElement<IMethod, LNode> Method =
            new CodecElement<IMethod, LNode>(
                CodeSymbols.Fn, IrMethod.Encode, IrMethod.Decode);

        /// <summary>
        /// Gets a codec that contains all sub-codecs defined in this class.
        /// </summary>
        /// <returns>A codec.</returns>
        public static Codec<ITypeMember, LNode> All
        {
            get
            {
                return new PiecewiseCodec<ITypeMember>()
                    .Add(Field)
                    .Add(Constructor)
                    .Add(Method);
            }
        }
    }
}
