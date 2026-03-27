using System;
using System.Runtime.InteropServices;

namespace LLVMSharp.Interop
{
    public static unsafe class InteropCompatExtensions
    {
        public static LLVMTypeRef CreateNamedStruct(this LLVMContextRef context, string name)
        {
            var namePtr = Marshal.StringToHGlobalAnsi(name);
            try
            {
                return new LLVMTypeRef((IntPtr)LLVM.StructCreateNamed(context, (sbyte*)namePtr));
            }
            finally
            {
                Marshal.FreeHGlobal(namePtr);
            }
        }

        public static LLVMTypeRef GetVoidTypeCompat(this LLVMContextRef context)
        {
            return new LLVMTypeRef((IntPtr)LLVM.VoidTypeInContext(context));
        }

        public static LLVMTypeRef GetInt1TypeCompat(this LLVMContextRef context)
        {
            return new LLVMTypeRef((IntPtr)LLVM.Int1TypeInContext(context));
        }

        public static LLVMTypeRef GetInt32TypeCompat(this LLVMContextRef context)
        {
            return new LLVMTypeRef((IntPtr)LLVM.Int32TypeInContext(context));
        }

        public static LLVMTypeRef CreateFunctionType(this LLVMTypeRef returnType, LLVMTypeRef[] parameterTypes, bool isVarArg)
        {
            fixed (LLVMTypeRef* parameterTypesPtr = parameterTypes)
            {
                return new LLVMTypeRef(
                    (IntPtr)LLVM.FunctionType(
                        returnType,
                        (LLVMOpaqueType**)parameterTypesPtr,
                        (uint)parameterTypes.Length,
                        isVarArg ? 1 : 0));
            }
        }

        public static LLVMTypeRef CreateStructType(this LLVMContextRef context, LLVMTypeRef[] elementTypes, bool packed)
        {
            fixed (LLVMTypeRef* elementTypesPtr = elementTypes)
            {
                return new LLVMTypeRef(
                    (IntPtr)LLVM.StructTypeInContext(
                        context,
                        (LLVMOpaqueType**)elementTypesPtr,
                        (uint)elementTypes.Length,
                        packed ? 1 : 0));
            }
        }

        public static void SetStructBody(this LLVMTypeRef type, LLVMTypeRef[] elementTypes, bool packed)
        {
            fixed (LLVMTypeRef* elementTypesPtr = elementTypes)
            {
                LLVM.StructSetBody(
                    type,
                    (LLVMOpaqueType**)elementTypesPtr,
                    (uint)elementTypes.Length,
                    packed ? 1 : 0);
            }
        }

        public static uint CountStructElementTypesCompat(this LLVMTypeRef type)
        {
            return LLVM.CountStructElementTypes(type);
        }

        public static LLVMValueRef CreateConstStruct(this LLVMContextRef context, LLVMValueRef[] values, bool packed)
        {
            fixed (LLVMValueRef* valuesPtr = values)
            {
                return new LLVMValueRef(
                    (IntPtr)LLVM.ConstStructInContext(
                        context,
                        (LLVMOpaqueValue**)valuesPtr,
                        (uint)values.Length,
                        packed ? 1 : 0));
            }
        }

        public static LLVMValueRef CreateConstInt(this LLVMTypeRef type, ulong value, bool signExtend)
        {
            return new LLVMValueRef((IntPtr)LLVM.ConstInt(type, value, signExtend ? 1 : 0));
        }

        public static LLVMValueRef CreateSizeOf(this LLVMTypeRef type)
        {
            return new LLVMValueRef((IntPtr)LLVM.SizeOf(type));
        }

        public static LLVMTypeRef CreateArrayType(this LLVMTypeRef elementType, uint elementCount)
        {
            return new LLVMTypeRef((IntPtr)LLVM.ArrayType(elementType, elementCount));
        }

        public static LLVMValueRef CreateConstArray(this LLVMTypeRef elementType, LLVMValueRef[] values)
        {
            fixed (LLVMValueRef* valuesPtr = values)
            {
                return new LLVMValueRef(
                    (IntPtr)LLVM.ConstArray(
                        elementType,
                        (LLVMOpaqueValue**)valuesPtr,
                        (uint)values.Length));
            }
        }

        public static uint GetEnumAttributeKindForNameCompat(string name)
        {
            var namePtr = Marshal.StringToHGlobalAnsi(name);
            try
            {
                return LLVM.GetEnumAttributeKindForName((sbyte*)namePtr, (nuint)name.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(namePtr);
            }
        }

        public static void SetLinkage(this LLVMValueRef value, LLVMLinkage linkage)
        {
            LLVM.SetLinkage(value, linkage);
        }

        public static void SetInitializer(this LLVMValueRef value, LLVMValueRef initializer)
        {
            LLVM.SetInitializer(value, initializer);
        }

        public static void SetGlobalConstant(this LLVMValueRef value, bool isConstant)
        {
            LLVM.SetGlobalConstant(value, isConstant ? 1 : 0);
        }

        public static void SetVolatile(this LLVMValueRef value, bool isVolatile)
        {
            LLVM.SetVolatile(value, isVolatile ? 1 : 0);
        }

        public static LLVMOpcode GetConstOpcode(this LLVMValueRef value)
        {
            return LLVM.GetConstOpcode(value);
        }

        public static IntPtr Pointer(this LLVMValueRef value)
        {
            return (IntPtr)(LLVMOpaqueValue*)value;
        }

        public static LLVMTypeKind TypeKind(this LLVMTypeRef type)
        {
            return type.Kind;
        }

        public static LLVMTypeRef GetReturnType(this LLVMTypeRef type)
        {
            return type.ReturnType;
        }

        public static string GetValueName(this LLVMValueRef value)
        {
            return Marshal.PtrToStringAnsi((IntPtr)LLVM.GetValueName(value)) ?? string.Empty;
        }
    }
}
