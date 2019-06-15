using Flame.Llvm.Emit;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// An abstract class for application-GC interfaces.
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
    }
}
