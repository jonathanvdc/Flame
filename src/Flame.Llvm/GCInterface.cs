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
        /// Emits instructions that load an object's metadata pointer.
        /// </summary>
        /// <param name="objectPointer">An object to inspect for its metadata pointer.</param>
        /// <param name="module">The module that defines the metadata-loading instructions.</param>
        /// <param name="builder">An instruction builder to use for emitting instructions.</param>
        /// <param name="name">A suggested name for the value that refers to the metadata.</param>
        /// <returns>A pointer to the metadata.</returns>
        public abstract LLVMValueRef EmitLoadMetadataPointer(
            LLVMValueRef objectPointer,
            ModuleBuilder module,
            IRBuilder builder,
            string name);
    }
}
