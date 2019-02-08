using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Supports creating, recognizing and parsing object-oriented intrinsics.
    /// </summary>
    public static class ObjectIntrinsics
    {
        /// <summary>
        /// The namespace for object-oriented intrinsics.
        /// </summary>
        public static readonly IntrinsicNamespace Namespace =
            new IntrinsicNamespace("object");

        /// <summary>
        /// Creates an object-oriented intrinsic prototype.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the operator represented by the object-oriented intrinsic.
        /// </param>
        /// <param name="resultType">
        /// The type of value produced by the intrinsic to create.
        /// </param>
        /// <param name="parameterTypes">
        /// The types of the values the intrinsic takes as arguments.
        /// </param>
        /// <returns>
        /// An object-oriented intrinsic prototype.
        /// </returns>
        public static IntrinsicPrototype CreatePrototype(
            string operatorName,
            IType resultType,
            IReadOnlyList<IType> parameterTypes)
        {
            return IntrinsicPrototype.Create(
                Namespace.GetIntrinsicName(operatorName),
                resultType,
                parameterTypes);
        }

        /// <summary>
        /// Creates an 'unbox_any' instruction prototype.
        /// Its return type can either be a value type or a
        /// reference type (aka box pointer).
        /// If its return type is set to a value type, 'unbox_any'
        /// unboxes its argument and loads it.
        /// If 'unbox_any's return value is set to a reference type,
        /// 'unbox_any' checks that its argument is a subtype of the
        /// return type.
        /// </summary>
        /// <param name="resultType">
        /// The 'unbox_any' instruction prototype's result type.
        /// </param>
        /// <param name="argumentType">
        /// The 'unbox_any' instruction prototype's argument type.
        /// </param>
        /// <returns>
        /// An 'unbox_any' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateUnboxAnyPrototype(
            IType resultType, IType argumentType)
        {
            // TODO: 'unbox_any' can actually only throw a bad cast exception.
            return CreatePrototype(
                Operators.UnboxAny,
                resultType,
                new[] { argumentType });
        }

        /// <summary>
        /// A collection of names for object-oriented operations.
        /// </summary>
        public static class Operators
        {
            /// <summary>
            /// The 'unbox_any' operator. Its return type can either
            /// be a value type or a reference type (aka box pointer).
            /// If its return type is set to a value type, 'unbox_any'
            /// unboxes its argument and loads it.
            /// If 'unbox_any's return value is set to a reference type,
            /// 'unbox_any' checks that its argument is a subtype of the
            /// return type.
            /// </summary>
            public const string UnboxAny = "unbox_any";

            /// <summary>
            /// An immutable array containing all standard object-oriented
            /// intrinsics.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        UnboxAny
                    });
        }
    }
}
