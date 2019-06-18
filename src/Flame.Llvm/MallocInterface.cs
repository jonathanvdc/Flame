using Flame.Llvm.Emit;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// A "GC" interface that uses the malloc function to allocate memory.
    /// No memory is ever freed.
    /// </summary>
    public sealed class MallocInterface : GCInterface
    {
        private MallocInterface()
        { }

        /// <summary>
        /// An instance of the malloc "GC" interface.
        /// </summary>
        public static readonly MallocInterface Instance
            = new MallocInterface();

        /// <inheritdoc/>
        public override LLVMValueRef EmitAllocObject(
            IType type,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            var metaType = GetMetadataExtendedType(module.ImportType(type), module);
            var metaVal = builder.CreateMalloc(metaType, name + ".alloc");
            builder.CreateStore(
                builder.CreateBitCast(
                    module.Metadata.GetMetadata(type, module),
                    GetMetadataPointerType(module),
                    "vtable.ptr"),
                builder.CreateStructGEP(metaVal, 0, "vtable.ptr.ref"));
            return builder.CreateStructGEP(metaVal, 1, name);
        }

        /// <inheritdoc/>
        public override LLVMValueRef EmitLoadMetadata(
            LLVMValueRef objectPointer,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            var castPointer = builder.CreateBitCast(
                objectPointer,
                LLVM.PointerType(GetMetadataPointerType(module), 0),
                "as.metadata.ptr.ref");
            var vtablePtrRef = builder.CreateGEP(
                castPointer,
                new[]
                {
                    LLVM.ConstInt(LLVM.Int32TypeInContext(module.Context), unchecked((ulong)-1), true)
                },
                "metadata.ptr.ref");
            return builder.CreateLoad(vtablePtrRef, name);
        }

        private static LLVMTypeRef GetMetadataExtendedType(
            LLVMTypeRef type,
            ModuleBuilder module)
        {
            return LLVM.StructType(
                new[]
                {
                    GetMetadataPointerType(module),
                    type
                },
                false);
        }

        private static LLVMTypeRef GetMetadataPointerType(ModuleBuilder module)
        {
            return LLVM.PointerType(LLVM.Int8TypeInContext(module.Context), 0);
        }
    }
}
