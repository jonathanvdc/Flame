using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Supports creating, recognizing and parsing exception handling intrinsics.
    /// </summary>
    public static class ExceptionIntrinsics
    {
        /// <summary>
        /// The namespace for exception handling intrinsics.
        /// </summary>
        public static readonly IntrinsicNamespace Namespace =
            new IntrinsicNamespace("exception");

        /// <summary>
        /// Creates an exception handling intrinsic prototype.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the operator represented by the exception handling intrinsic.
        /// </param>
        /// <param name="resultType">
        /// The type of value produced by the intrinsic to create.
        /// </param>
        /// <param name="parameterTypes">
        /// The types of the values the intrinsic takes as arguments.
        /// </param>
        /// <param name="exceptionSpec">
        /// The exception specification of the intrinsic.
        /// </param>
        /// <returns>
        /// An exception handling intrinsic prototype.
        /// </returns>
        public static IntrinsicPrototype CreatePrototype(
            string operatorName,
            IType resultType,
            IReadOnlyList<IType> parameterTypes,
            ExceptionSpecification exceptionSpec)
        {
            return IntrinsicPrototype.Create(
                Namespace.GetIntrinsicName(operatorName),
                resultType,
                parameterTypes,
                exceptionSpec);
        }

        /// <summary>
        /// Creates a 'throw' instruction prototype,
        /// which throws an exception.
        /// </summary>
        /// <param name="exceptionType">
        /// The type of exception to throw.
        /// </param>
        /// <returns>
        /// A 'throw' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateThrowPrototype(
            IType exceptionType)
        {
            return CreatePrototype(
                Operators.Throw,
                exceptionType,
                new[] { exceptionType },
                ExceptionSpecification.ThrowAny);
        }

        /// <summary>
        /// A collection of names for exception handling operations.
        /// </summary>
        public static class Operators
        {
            /// <summary>
            /// The 'throw' operator, which throws a new exception.
            /// </summary>
            public const string Throw = "throw";

            /// <summary>
            /// An immutable array containing all standard exception handling
            /// intrinsics.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        Throw
                    });
        }
    }
}
