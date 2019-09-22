using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Collections;
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
        /// Creates an instruction prototype that allocates a function-local
        /// variable that is pinned; the GC is not allowed to move the contents
        /// of the local.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to store in the pinned variable.
        /// </param>
        /// <returns>An alloca-pinned instruction prototype.</returns>
        public static IntrinsicPrototype CreateAllocaPinnedPrototype(
            IType elementType)
        {
            return CreatePrototype(
                Operators.AllocaPinned,
                elementType.MakePointerType(PointerKind.Reference),
                EmptyArray<IType>.Value);
        }

        /// <summary>
        /// A collection of names for memory operations.
        /// </summary>
        public static class Operators
        {
            /// <summary>
            /// An operator that allocates a function-local variable containing a pinned
            /// pointer.
            /// </summary>
            public const string AllocaPinned = "alloca_pinned";

            /// <summary>
            /// An immutable array containing all standard memory intrinsics.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        AllocaPinned
                    });
        }
    }
}
