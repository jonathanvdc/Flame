using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
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
            return EmitAllocObject(type, module.ImportType(type), module, builder, name);
        }

        private static LLVMValueRef EmitAllocObject(
            IType type,
            LLVMTypeRef importedType,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            var metaType = GetMetadataExtendedType(importedType, module);
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

        /// <inheritdoc/>
        public override LLVMValueRef EmitAllocDelegate(
            IType type,
            LLVMValueRef callee,
            LLVMValueRef thisArgument,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            // Allocate the delegate object.
            var ptr = EmitAllocObject(type, module, builder, name);

            // Decompose the delegate into its three main fields.
            var fieldPtrs = DecomposeDelegateObject(ptr, type, module, builder);

            // Set the 'method_ptr' field to the callee pointer.
            CreateStoreAnyPtr(callee, fieldPtrs.MethodPtrPtr, builder);
            if (thisArgument.Pointer == IntPtr.Zero)
            {
                // If there is no 'this' argument, then we need to create a small thunk that discards
                // the 'this' argument. We store this thunk in the 'invoke_impl' field.
                var thunk = GetDelegateThunk(
                    callee,
                    fieldPtrs.TargetPtr.TypeOf().GetElementType(),
                    module);
                CreateStoreAnyPtr(thunk, fieldPtrs.InvokeImplPtr, builder);
            }
            else
            {
                // If there is a 'this' argument, then we simply set the 'target' and 'invoke_impl' fields.
                CreateStoreAnyPtr(thisArgument, fieldPtrs.TargetPtr, builder);
                CreateStoreAnyPtr(callee, fieldPtrs.InvokeImplPtr, builder);
            }

            return ptr;
        }

        private static LLVMValueRef GetDelegateThunk(
            LLVMValueRef callee,
            LLVMTypeRef targetParamType,
            ModuleBuilder module)
        {
            var thunkName = callee.GetValueName();
            int startIndex = thunkName.IndexOf('@');
            int endIndex = thunkName.IndexOf('(');
            if (startIndex >= 0)
            {
                thunkName = thunkName.Substring(startIndex + 1, endIndex - startIndex - 1);
            }
            thunkName += ".thunk";
            var thunk = LLVM.GetNamedFunction(module.Module, thunkName);
            if (thunk.Pointer == IntPtr.Zero)
            {
                var signature = callee.TypeOf().GetElementType();
                var thunkParams = new List<LLVMTypeRef>();
                thunkParams.Add(targetParamType);
                thunkParams.AddRange(signature.GetParamTypes());
                thunk = LLVM.AddFunction(
                    module.Module,
                    thunkName,
                    LLVM.FunctionType(
                        signature.GetReturnType(),
                        thunkParams.ToArray(),
                        signature.IsFunctionVarArg));

                using (var builder = new IRBuilder(module.Context))
                {
                    builder.PositionBuilderAtEnd(thunk.AppendBasicBlock("entry"));
                    var result = builder.CreateCall(callee, thunk.GetParams().Skip(1).ToArray(), "");
                    if (result.TypeOf().TypeKind == LLVMTypeKind.LLVMVoidTypeKind)
                    {
                        builder.CreateRetVoid();
                    }
                    else
                    {
                        builder.CreateRet(result);
                    }
                }
            }
            return thunk;
        }

        private static void CreateStoreAnyPtr(LLVMValueRef value, LLVMValueRef ptr, IRBuilder builder)
        {
            builder.CreateStore(builder.CreateBitCast(value, ptr.TypeOf().GetElementType(), ""), ptr);
        }

        private static DelegateTriple DecomposeDelegateObject(
            LLVMValueRef obj,
            IType type,
            ModuleBuilder module,
            IRBuilder builder)
        {
            // Peel away at the inheritance hierarchy until we reach a base type
            // that defines critical fields.
            IField invokeImplField = null;
            IField targetField = null;
            IField methodPtrField = null;
            var baseType = type;
            while (baseType != null)
            {
                invokeImplField = baseType.Fields.FirstOrDefault(f => f.Name.ToString() == "invoke_impl");
                if (invokeImplField != null)
                {
                    targetField = baseType.Fields.First(f => f.Name.ToString() == "m_target");
                    methodPtrField = baseType.Fields.First(f => f.Name.ToString() == "method_ptr");
                    break;
                }
                else
                {
                    baseType = baseType.BaseTypes.FirstOrDefault(t => !t.IsInterfaceType());
                }
            }
            if (baseType == null)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName.ToString()} was not recognized as a delegate " +
                    "because it does not define a field named 'invoke_impl'.");
            }

            // Cast the delegate instance pointer to that base type.
            var basePtr = builder.CreateBitCast(obj, LLVM.PointerType(module.ImportType(baseType), 0), "");

            // Create field pointers.
            var result = new DelegateTriple();
            result.MethodPtrPtr = builder.CreateStructGEP(basePtr, (uint)module.GetFieldIndex(methodPtrField), "");
            result.InvokeImplPtr = builder.CreateStructGEP(basePtr, (uint)module.GetFieldIndex(invokeImplField), "");
            result.TargetPtr = builder.CreateStructGEP(basePtr, (uint)module.GetFieldIndex(targetField), "");
            return result;
        }

        private struct DelegateTriple
        {
            public LLVMValueRef MethodPtrPtr;
            public LLVMValueRef InvokeImplPtr;
            public LLVMValueRef TargetPtr;
        }
    }
}
