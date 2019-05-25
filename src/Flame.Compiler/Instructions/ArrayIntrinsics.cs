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
        /// <returns>
        /// An array intrinsic prototype.
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
        /// A 'get_element_pointer' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateGetElementPointerPrototype(
            IType elementType, IType arrayType, IReadOnlyList<IType> indexTypes)
        {
            return CreatePrototype(
                Operators.GetElementPointer,
                elementType.MakePointerType(PointerKind.Reference),
                new[] { arrayType }.Concat(indexTypes).ToArray());
        }

        /// <summary>
        /// Creates a 'load_element' instruction prototype,
        /// which indexes an array and loads the indexed array element.
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
        /// A 'load_element' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateLoadElementPrototype(
            IType elementType, IType arrayType, IReadOnlyList<IType> indexTypes)
        {
            return CreatePrototype(
                Operators.LoadElement,
                elementType,
                new[] { arrayType }.Concat(indexTypes).ToArray());
        }

        /// <summary>
        /// Creates a 'store_element' instruction prototype,
        /// which indexes an array and updates the indexed array element.
        /// </summary>
        /// <param name="elementType">
        /// The type of element to store in the array.
        /// </param>
        /// <param name="arrayType">
        /// The type of array to index.
        /// </param>
        /// <param name="indexTypes">
        /// The types of indices to index the array with.
        /// </param>
        /// <returns>
        /// A 'store_element' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateStoreElementPrototype(
            IType elementType, IType arrayType, IReadOnlyList<IType> indexTypes)
        {
            return CreatePrototype(
                Operators.StoreElement,
                elementType,
                new[] { elementType, arrayType }.Concat(indexTypes).ToArray());
        }

        /// <summary>
        /// Creates a 'get_length' instruction prototype,
        /// which computes the number of elements in an array.
        /// </summary>
        /// <param name="sizeType">
        /// The type of integer to store the length of the array in.
        /// </param>
        /// <param name="arrayType">
        /// The type of array to inspect.
        /// </param>
        /// <returns>
        /// A 'get_length' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateGetLengthPrototype(
            IType sizeType, IType arrayType)
        {
            return CreatePrototype(
                Operators.GetLength,
                sizeType,
                new[] { arrayType });
        }

        /// <summary>
        /// Creates a 'new_array' instruction prototype,
        /// allocates a new array.
        /// </summary>
        /// <param name="arrayType">
        /// The type of array to create.
        /// </param>
        /// <param name="sizeType">
        /// The type of integer that describes the length of the
        /// array to create.
        /// </param>
        /// <returns>
        /// A 'new_array' instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateNewArrayPrototype(
            IType arrayType, IType sizeType)
        {
            return CreatePrototype(
                Operators.NewArray,
                arrayType,
                new[] { sizeType });
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
            /// The 'store_element' operator, which indexes
            /// an array and updates the indexed array element.
            /// </summary>
            public const string StoreElement = "store_element";

            /// <summary>
            /// The 'get_length' operator, which computes the number
            /// of elements in an array.
            /// </summary>
            public const string GetLength = "get_length";

            /// <summary>
            /// The 'new_array' operator, which allocates a new array
            /// of a particular size.
            /// </summary>
            public const string NewArray = "new_array";

            /// <summary>
            /// An immutable array containing all standard array
            /// intrinsics.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        GetElementPointer,
                        LoadElement,
                        StoreElement,
                        GetLength,
                        NewArray
                    });
        }
    }
}
