using Flame.Llvm.Emit;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// A description of a member metadata format. Member metadata is
    /// used for type instance tests, virtual method lookup and reflection.
    /// </summary>
    public abstract class MetadataFormat
    {
        /// <summary>
        /// Gets a pointer to the metadata for a particular type.
        /// </summary>
        /// <param name="type">The type whose metadata is to be inspected.</param>
        /// <param name="module">
        /// The LLVM module to generate the metadata in. This module must be the
        /// same for all calls to the metadata format description.
        /// </param>
        /// <returns>A metadata pointer.</returns>
        public abstract LLVMValueRef GetMetadataPointer(IType type, ModuleBuilder module);
    }
}
