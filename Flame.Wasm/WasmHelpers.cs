using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Wasm;
using WasmAnyType = Wasm.WasmType;

namespace Flame.Wasm
{
    public static class WasmHelpers
    {
        private static readonly Dictionary<IType, WasmValueType> scalarValueTypes = new Dictionary<IType, WasmValueType>()
        {
            { PrimitiveTypes.Int8, WasmValueType.Int32 },
            { PrimitiveTypes.Int16, WasmValueType.Int32 },
            { PrimitiveTypes.Int32, WasmValueType.Int32 },
            { PrimitiveTypes.Int64, WasmValueType.Int64 },
            { PrimitiveTypes.UInt8, WasmValueType.Int32 },
            { PrimitiveTypes.UInt16, WasmValueType.Int32 },
            { PrimitiveTypes.UInt32, WasmValueType.Int32 },
            { PrimitiveTypes.UInt64, WasmValueType.Int64 },
            { PrimitiveTypes.Bit8, WasmValueType.Int32 },
            { PrimitiveTypes.Bit16, WasmValueType.Int32 },
            { PrimitiveTypes.Bit32, WasmValueType.Int32 },
            { PrimitiveTypes.Bit64, WasmValueType.Int64 },
            { PrimitiveTypes.Boolean, WasmValueType.Int32 },
            { PrimitiveTypes.Char, WasmValueType.Int32 },
            { PrimitiveTypes.Float32, WasmValueType.Float32 },
            { PrimitiveTypes.Float64, WasmValueType.Float64 }
        };

        /// <summary>
        /// Gets the WebAssembly value type that corresponds to the given scalar type.
        /// </summary>
        public static WasmValueType GetWasmValueType(IType Type, IAbi Abi)
        {
            if (Type.GetIsPointer())
                return scalarValueTypes[Abi.PointerIntegerType];
            else
                return scalarValueTypes[Type];
        }
        
        /// <summary>
        /// Gets the WebAssembly type that corresponds to the given type.
        /// </summary>
        public static WasmAnyType GetWasmType(IType Type, IAbi Abi)
        {
            if (Type.Equals(PrimitiveTypes.Void))
                return WasmAnyType.Empty;
            else
                return (WasmAnyType)GetWasmValueType(Type, Abi);
        }

        public static string GetWasmName(IType Type)
        {
            if (Type == null)
                return "";
            return Type.FullName.ToString();
        }

        public static string GetWasmName(IMethod Method)
        {
            if (Method == null)
                return "";
            return MemberExtensions.CombineNames(GetWasmName(Method.DeclaringType), Method.Name.ToString());
        }

        public static bool IsScalar(this IType Type)
        {
            return Type.GetIsPrimitive() || Type.GetIsPointer();
        }
    }
}
