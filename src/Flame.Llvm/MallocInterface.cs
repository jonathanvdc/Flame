using System.Collections.Generic;
using System.Linq;
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
                module.Metadata.GetMetadata(type, module),
                builder.CreateStructGEP(metaVal, 0, "metadata.ref"));
            return builder.CreateStructGEP(metaVal, 1, name);
        }

        /// <inheritdoc/>
        public override LLVMValueRef EmitAllocArray(
            IType arrayType,
            IType elementType,
            IReadOnlyList<LLVMValueRef> dimensions,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            var llvmElementType = module.ImportType(elementType);
            var lengthType = GetArrayLengthType(module);
            var headerType = GetArrayHeaderType(elementType, dimensions.Count, module);

            var headerFields = dimensions.Select(x => builder.CreateIntCast(x, lengthType, "")).ToArray();
            var metaType = GetMetadataExtendedType(headerType, module);
            var totalSize = headerFields.Aggregate(LLVM.SizeOf(llvmElementType), (x, y) => builder.CreateMul(x, y, ""));
            var bytes = builder.CreateArrayMalloc(
                LLVM.Int8TypeInContext(module.Context),
                builder.CreateAdd(LLVM.SizeOf(metaType), totalSize, ""),
                name + ".alloc");

            // Set the array's metadata.
            var metaVal = builder.CreateBitCast(bytes, LLVM.PointerType(metaType, 0), "");
            builder.CreateStore(
                module.Metadata.GetMetadata(arrayType, module),
                builder.CreateStructGEP(metaVal, 0, "metadata.ref"));

            var headerPtr = builder.CreateStructGEP(metaVal, 1, "");
            var result = builder.CreateBitCast(
                headerPtr,
                LLVM.PointerType(module.ImportType(arrayType), 0),
                name);

            // Set the array header.
            for (int i = 0; i < headerFields.Length; i++)
            {
                builder.CreateStore(
                    headerFields[i],
                    builder.CreateStructGEP(headerPtr, (uint)i, ""));
            }

            return result;
        }

        private static LLVMTypeRef GetArrayHeaderType(
            IType elementType,
            int dimensions,
            ModuleBuilder module)
        {
            return LLVM.StructTypeInContext(
                module.Context,
                Enumerable.Repeat(GetArrayLengthType(module), dimensions)
                    .Concat(new[] { LLVM.ArrayType(module.ImportType(elementType), 0) })
                    .ToArray(),
                false);
        }

        private static LLVMTypeRef GetArrayLengthType(ModuleBuilder module)
        {
            return LLVM.Int64TypeInContext(module.Context);
        }

        /// <inheritdoc/>
        public override LLVMValueRef EmitArrayElementAddress(
            LLVMValueRef array,
            IType elementType,
            IReadOnlyList<LLVMValueRef> indices,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            var llvmElementType = module.ImportType(elementType);
            var lengthType = GetArrayLengthType(module);
            var headerType = GetArrayHeaderType(elementType, indices.Count, module);

            var headerPtr = builder.CreateBitCast(array, LLVM.PointerType(headerType, 0), "");
            var dataPtr = builder.CreateBitCast(
                builder.CreateStructGEP(headerPtr, (uint)indices.Count, ""),
                LLVM.PointerType(llvmElementType, 0),
                "data.ptr");

            var index = builder.CreateIntCast(indices[0], lengthType, "");
            for (int i = 0; i < indices.Count - 1; i++)
            {
                var dim = builder.CreateLoad(builder.CreateStructGEP(headerPtr, (uint)i, ""), "dim." + i);
                index = builder.CreateAdd(
                    builder.CreateMul(index, dim, ""),
                    builder.CreateIntCast(indices[i + 1], lengthType, ""),
                    "");
            }

            return builder.CreateGEP(dataPtr, new[] { index }, name);
        }

        /// <inheritdoc/>
        public override LLVMValueRef EmitArrayLength(
            LLVMValueRef array,
            IType elementType,
            int dimensions,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            var llvmElementType = module.ImportType(elementType);
            var lengthType = GetArrayLengthType(module);
            var headerType = GetArrayHeaderType(elementType, dimensions, module);
            var headerPtr = builder.CreateBitCast(array, LLVM.PointerType(headerType, 0), "header.ptr");

            return Enumerable.Range(0, dimensions).Aggregate(
                LLVM.ConstInt(lengthType, 1, false),
                (acc, i) => builder.CreateMul(acc, builder.CreateLoad(builder.CreateStructGEP(headerPtr, (uint)i, ""), ""), ""));
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
                LLVM.PointerType(module.Metadata.GetMetadataType(module), 0),
                "as.metadata.ref");
            var vtablePtrRef = builder.CreateGEP(
                castPointer,
                new[]
                {
                    LLVM.ConstInt(LLVM.Int32TypeInContext(module.Context), unchecked((ulong)-1), true)
                },
                "metadata.ref");
            return builder.CreateLoad(vtablePtrRef, name);
        }
    }
}
