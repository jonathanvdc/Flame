using System.Collections.Generic;
using Flame.Llvm.Emit;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// An application-GC interface, which defines how the application
    /// interacts with the GC. The application-GC interface's responsibilities
    /// include root set management, object allocation and the object header format.
    /// </summary>
    public abstract class GCInterface
    {
        /// <summary>
        /// Emits instructions that allocate a GC-managed instance of a type.
        /// </summary>
        /// <param name="type">A type to instantiate.</param>
        /// <param name="module">The module that defines the object-allocating instructions.</param>
        /// <param name="builder">An instruction builder to use for emitting instructions.</param>
        /// <param name="name">A suggested name for the value that refers to the allocated object.</param>
        /// <returns>A pointer to the allocated object.</returns>
        public abstract LLVMValueRef EmitAllocObject(
            IType type,
            ModuleBuilder module,
            IRBuilder builder,
            string name);

        /// <summary>
        /// Emits LLVM IR instructions that allocate a new array.
        /// </summary>
        /// <param name="arrayType">
        /// The type of the array to allocate.
        /// </param>
        /// <param name="elementType">
        /// The type of the values stored in the array.
        /// </param>
        /// <param name="dimensions">
        /// The array's dimensions, as sequence of integer values.
        /// </param>
        /// <param name="module">
        /// The LLVM module to generate the instructions in.
        /// </param>
        /// <param name="builder">
        /// An instruction builder to use for emitting instructions.
        /// </param>
        /// <param name="name">
        /// A suggested name for the resulting array pointer.
        /// </param>
        /// <returns>A value that points to an array.</returns>
        public abstract LLVMValueRef EmitAllocArray(
            IType arrayType,
            IType elementType,
            IReadOnlyList<LLVMValueRef> dimensions,
            ModuleBuilder module,
            IRBuilder builder,
            string name);

        /// <summary>
        /// Emits instructions that load an object's metadata handle.
        /// </summary>
        /// <param name="objectPointer">An object to inspect for its metadata handle.</param>
        /// <param name="module">The module that defines the metadata-loading instructions.</param>
        /// <param name="builder">An instruction builder to use for emitting instructions.</param>
        /// <param name="name">A suggested name for the value that refers to the metadata.</param>
        /// <returns>A handle to the metadata.</returns>
        public abstract LLVMValueRef EmitLoadMetadata(
            LLVMValueRef objectPointer,
            ModuleBuilder module,
            IRBuilder builder,
            string name);

        /// <summary>
        /// Emits LLVM IR instructions that load the address of an element
        /// in an array.
        /// </summary>
        /// <param name="array">
        /// The array value to inspect.
        /// </param>
        /// <param name="elementType">
        /// The type of the values stored in the array.
        /// </param>
        /// <param name="indices">
        /// The indices into <paramref name="array"/>, as sequence of integer values.
        /// </param>
        /// <param name="module">
        /// The LLVM module to generate the instructions in.
        /// </param>
        /// <param name="builder">
        /// An instruction builder to use for emitting instructions.
        /// </param>
        /// <param name="name">
        /// A suggested name for the resulting array pointer.
        /// </param>
        /// <returns>A value that points to an array.</returns>
        public abstract LLVMValueRef EmitArrayElementAddress(
            LLVMValueRef array,
            IType elementType,
            IReadOnlyList<LLVMValueRef> indices,
            ModuleBuilder module,
            IRBuilder builder,
            string name);

        /// <summary>
        /// Emits LLVM IR instructions that compute an array's total length,
        /// that is, the product of its dimensions.
        /// </summary>
        /// <param name="array">
        /// The array value to inspect.
        /// </param>
        /// <param name="elementType">
        /// The type of the values stored in the array.
        /// </param>
        /// <param name="dimensions">
        /// The dimensionality of <paramref name="array"/>.
        /// </param>
        /// <param name="module">
        /// The LLVM module to generate the instructions in.
        /// </param>
        /// <param name="builder">
        /// An instruction builder to use for emitting instructions.
        /// </param>
        /// <param name="name">
        /// A suggested name for the resulting array pointer.
        /// </param>
        /// <returns>The product of <paramref name="array"/>'s dimensions.</returns>
        public abstract LLVMValueRef EmitArrayLength(
            LLVMValueRef array,
            IType elementType,
            int dimensions,
            ModuleBuilder module,
            IRBuilder builder,
            string name);

        internal static LLVMTypeRef GetMetadataExtendedType(
            LLVMTypeRef type,
            ModuleBuilder module)
        {
            return LLVM.StructTypeInContext(
                module.Context,
                new[]
                {
                    module.Metadata.GetMetadataType(module),
                    type
                },
                false);
        }
    }
}
