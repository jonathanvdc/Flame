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
        /// <returns>
        /// An exception handling intrinsic prototype.
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
        /// Creates a 'capture' instruction prototype,
        /// which captures a (thrown) exception.
        /// </summary>
        /// <param name="resultType">
        /// The type of a captured exception.
        /// </param>
        /// <param name="argumentType">
        /// The type of the exception to capture.
        /// </param>
        /// <returns>
        /// A 'capture' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateCapturePrototype(
            IType resultType,
            IType argumentType)
        {
            return CreatePrototype(
                Operators.Capture,
                resultType,
                new[] { argumentType });
        }

        /// <summary>
        /// Creates a 'get_captured_exception' instruction prototype,
        /// which extracts the exception captured by a
        /// captured exception.
        /// </summary>
        /// <param name="resultType">
        /// The type of the exception value returned
        /// by this operation.
        /// </param>
        /// <param name="argumentType">
        /// The type of the captured exception to examine.
        /// </param>
        /// <returns>
        /// A 'get_captured_exception' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateGetCapturedExceptionPrototype(
            IType resultType,
            IType argumentType)
        {
            return CreatePrototype(
                Operators.GetCapturedException,
                resultType,
                new[] { argumentType });
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
                new[] { exceptionType });
        }

        /// <summary>
        /// Creates a 'rethrow' instruction prototype, which rethrows a
        /// captured exception. The difference between 'rethrow' and 'throw'
        /// is that the former takes a captured exception and retains stack
        /// trace information whereas the latter takes a (raw) exception value
        /// and constructs a new stack trace.
        /// </summary>
        /// <param name="capturedExceptionType">
        /// The type of the captured exception to rethrow.
        /// </param>
        /// <returns>A 'rethrow' intrinsic.</returns>
        public static IntrinsicPrototype CreateRethrowPrototype(
            IType capturedExceptionType)
        {
            return CreatePrototype(
                Operators.Rethrow,
                capturedExceptionType,
                new[] { capturedExceptionType });
        }

        /// <summary>
        /// A collection of names for exception handling operations.
        /// </summary>
        public static class Operators
        {
            /// <summary>
            /// The 'capture' operator, which captures a (thrown) exception.
            /// Captured exceptions can be rethrown.
            /// </summary>
            public const string Capture = "capture";

            /// <summary>
            /// The 'get_captured_exception' operator, which extracts the exception
            /// captured by a captured exception.
            /// </summary>
            public const string GetCapturedException = "get_captured_exception";

            /// <summary>
            /// The 'throw' operator, which throws a new exception.
            /// </summary>
            public const string Throw = "throw";

            /// <summary>
            /// The 'rethrow' operator, which rethrows an existing exception.
            /// </summary>
            public const string Rethrow = "rethrow";

            /// <summary>
            /// An immutable array containing all standard exception handling
            /// intrinsics.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        Capture,
                        GetCapturedException,
                        Throw,
                        Rethrow
                    });
        }
    }
}
