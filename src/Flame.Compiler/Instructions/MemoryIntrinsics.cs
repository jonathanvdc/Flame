using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Supports creating, recognizing and parsing memory manipulation intrinsics.
    /// </summary>
    public static class MemoryIntrinsics
    {
        /// <summary>
        /// The namespace for intrinsics that inspect or manipulate memory.
        /// </summary>
        public static readonly IntrinsicNamespace Namespace =
            new IntrinsicNamespace("memory");

        /// <summary>
        /// Creates a memory intrinsic prototype.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the operator represented by the memory intrinsic.
        /// </param>
        /// <param name="resultType">
        /// The type of value produced by the intrinsic to create.
        /// </param>
        /// <param name="parameterTypes">
        /// The types of the values the intrinsic takes as arguments.
        /// </param>
        /// <returns>
        /// A memory intrinsic prototype.
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
        /// Creates an instruction prototype for volatile loads.
        /// </summary>
        /// <param name="pointerType">
        /// The type of the pointer to dereference.
        /// </param>
        /// <returns>
        /// A volatile load instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateVolatileLoadPrototype(
            PointerType pointerType)
        {
            return CreatePrototype(
                Operators.VolatileLoad,
                pointerType.ElementType,
                new[] { pointerType });
        }

        /// <summary>
        /// Creates an instruction prototype for volatile stores.
        /// </summary>
        /// <param name="pointerType">
        /// The type of the pointer to dereference.
        /// </param>
        /// <returns>
        /// A volatile store instruction prototype.
        /// </returns>
        public static IntrinsicPrototype CreateVolatileStorePrototype(
            PointerType pointerType)
        {
            return CreatePrototype(
                Operators.VolatileStore,
                pointerType.ElementType,
                new[] { pointerType, pointerType.ElementType });
        }

        /// <summary>
        /// A collection of names for memory operations.
        /// </summary>
        public static class Operators
        {
            /// <summary>
            /// The volatile load operator, which loads a value from an address.
            /// Volatile loads must not be reordered with regard to other memory
            /// operations.
            /// </summary>
            public const string VolatileLoad = "volatile_load";

            /// <summary>
            /// The volatile stores operator, which stores a value at an address.
            /// Volatile stores must not be reordered with regard to other memory
            /// operations.
            /// </summary>
            public const string VolatileStore = "volatile_store";

            /// <summary>
            /// An immutable array containing all standard memory intrinsics.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        VolatileLoad,
                        VolatileStore
                    });
        }
    }
}
