using System.Collections.Generic;
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

        /// <summary>
        /// Builds LLVM IR instructions that perform a virtual method lookup:
        /// loads the address of the implementation of a virtual method given
        /// a type metadata pointer for the 'this' type.
        /// </summary>
        /// <param name="callee">
        /// A virtual method to find an implementation for.
        /// </param>
        /// <param name="metadataPointer">
        /// A pointer to the type metadata of the 'this' type.
        /// </param>
        /// <param name="module">
        /// The LLVM module to generate the instructions in. This module must be the
        /// same for all calls to the metadata format description.
        /// </param>
        /// <param name="builder">
        /// An instruction builder to use for emitting instructions.
        /// </param>
        /// <param name="name">
        /// A suggested name for the value that refers to the method
        /// implementation address.
        /// </param>
        /// <returns>A pointer to a method implementation.</returns>
        public abstract LLVMValueRef LookupVirtualMethod(
            IMethod callee,
            LLVMValueRef metadataPointer,
            ModuleBuilder module,
            IRBuilder builder,
            string name);
    }
}
