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
        /// Gets the type of a type metadata handle.
        /// </summary>
        /// <param name="module">A module.</param>
        /// <returns>The type of a type metadata handle.</returns>
        public abstract LLVMTypeRef GetMetadataType(ModuleBuilder module);

        /// <summary>
        /// Builds LLVM IR instructions that get a handle to the metadata
        /// for a particular type.
        /// </summary>
        /// <param name="type">The type whose metadata is to be inspected.</param>
        /// <param name="module">
        /// The LLVM module to generate the metadata in.
        /// </param>
        /// <param name="builder">
        /// An instruction builder to use for emitting instructions.
        /// </param>
        /// <param name="name">
        /// A suggested name for the value that refers to the metadata.
        /// </param>
        /// <returns>A metadata pointer.</returns>
        public abstract LLVMValueRef GetMetadata(
            IType type,
            ModuleBuilder module,
            IRBuilder builder,
            string name);

        /// <summary>
        /// Builds LLVM IR instructions that perform a virtual method lookup:
        /// loads the address of the implementation of a virtual method given
        /// a type metadata pointer for the 'this' type.
        /// </summary>
        /// <param name="callee">
        /// A virtual method to find an implementation for.
        /// </param>
        /// <param name="metadata">
        /// A handle to the type metadata of the 'this' type.
        /// </param>
        /// <param name="module">
        /// The LLVM module to generate the instructions in.
        /// </param>
        /// <param name="builder">
        /// An instruction builder to use for emitting instructions.
        /// </param>
        /// <param name="name">
        /// A suggested name for the value that refers to the method
        /// implementation address.
        /// </param>
        /// <returns>A pointer to a method implementation.</returns>
        public abstract LLVMValueRef EmitMethodAddress(
            IMethod callee,
            LLVMValueRef metadata,
            ModuleBuilder module,
            IRBuilder builder,
            string name);

        /// <summary>
        /// Emits LLVM IR instructions that test if the type corresponding
        /// to a type metadata handle is a subtype of another type.
        /// </summary>
        /// <param name="subtypeMetadata">
        /// A type metadata handle of a potential subtype.
        /// </param>
        /// <param name="supertype">
        /// A potential supertype.
        /// </param>
        /// <param name="module">
        /// The LLVM module to generate the instructions in.
        /// </param>
        /// <param name="builder">
        /// An instruction builder to use for emitting instructions.
        /// </param>
        /// <param name="name">
        /// A suggested name for the resulting Boolean value.
        /// </param>
        /// <returns>
        /// A Boolean value that is <c>true</c> if the type corresponding
        /// to <paramref name="subtypeMetadata"/> is a subtype of <paramref name="supertype"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public abstract LLVMValueRef EmitIsSubtype(
            LLVMValueRef subtypeMetadata,
            IType supertype,
            ModuleBuilder module,
            IRBuilder builder,
            string name);
    }
}
