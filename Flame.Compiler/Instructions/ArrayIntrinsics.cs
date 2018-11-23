using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Supports creating, recognizing and parsing array-related intrinsics.
    /// </summary>
    public static class ArrayIntrinsics
    {
        /// <summary>
        /// The namespace for array intrinsics.
        /// </summary>
        public static readonly IntrinsicNamespace Namespace =
            new IntrinsicNamespace("array");

        /// <summary>
        /// Creates an array intrinsic prototype.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the operator represented by the array intrinsic.
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
        /// An array intrinsic prototype.
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
        /// Creates a 'get_element_pointer' instruction prototype,
        /// which indexes an array and produces a reference to the
        /// indexed array element.
        /// </summary>
        /// <param name="elementType">
        /// The type of element to create a reference to.
        /// </param>
        /// <param name="arrayType">
        /// The type of array to index.
        /// </param>
        /// <param name="indexTypes">
        /// The types of indices to index the array with.
        /// </param>
        /// <returns>
        /// An 'get_element_pointer' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateGetElementPointerPrototype(
            IType elementType, IType arrayType, IReadOnlyList<IType> indexTypes)
        {
            return CreatePrototype(
                Operators.GetElementPointer,
                elementType.MakePointerType(PointerKind.Reference),
                new[] { arrayType }.Concat(indexTypes).ToArray(),
                ExceptionSpecification.ThrowAny);
        }

        /// <summary>
        /// Creates a 'load_element' instruction prototype,
        /// which indexes an array and produces a reference to the
        /// indexed array element.
        /// </summary>
        /// <param name="elementType">
        /// The type of element to load.
        /// </param>
        /// <param name="arrayType">
        /// The type of array to index.
        /// </param>
        /// <param name="indexTypes">
        /// The types of indices to index the array with.
        /// </param>
        /// <returns>
        /// An 'load_element' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateLoadElementPrototype(
            IType elementType, IType arrayType, IReadOnlyList<IType> indexTypes)
        {
            return CreatePrototype(
                Operators.LoadElement,
                elementType,
                new[] { arrayType }.Concat(indexTypes).ToArray(),
                ExceptionSpecification.ThrowAny);
        }

        /// <summary>
        /// A collection of names for array operations.
        /// </summary>
        public static class Operators
        {
            /// <summary>
            /// The 'get_element_pointer' operator, which indexes
            /// an array and produces a reference to the indexed array
            /// element.
            /// </summary>
            public const string GetElementPointer = "get_element_pointer";

            /// <summary>
            /// The 'load_element' operator, which indexes
            /// an array and loads the indexed array element.
            /// </summary>
            public const string LoadElement = "load_element";

            /// <summary>
            /// An immutable array containing all standard array
            /// intrinsics.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        GetElementPointer,
                        LoadElement
                    });
        }
    }
}
