using System;
using System.Collections.Generic;

namespace Flame.Wasm
{
	public static class WasmHelpers
	{
		private static readonly Dictionary<IType, string> scalarWasmNames = new Dictionary<IType, string>()
		{
			{ PrimitiveTypes.Int8, "i32" },
			{ PrimitiveTypes.Int16, "i32" },
			{ PrimitiveTypes.Int32, "i32" },
			{ PrimitiveTypes.Int64, "i64" },
			{ PrimitiveTypes.UInt8, "i32" },
			{ PrimitiveTypes.UInt16, "i32" },
			{ PrimitiveTypes.UInt32, "i32" },
			{ PrimitiveTypes.UInt64, "i64" },
			{ PrimitiveTypes.Bit8, "i32" },
			{ PrimitiveTypes.Bit16, "i32" },
			{ PrimitiveTypes.Bit32, "i32" },
			{ PrimitiveTypes.Bit64, "i64" },
			{ PrimitiveTypes.Boolean, "i32" },
			{ PrimitiveTypes.Char, "i32" }
		};

		public static string GetScalarWasmName(IType Type)
		{
			if (Type.GetIsPointer())
				return "i32";
			else
				return scalarWasmNames[Type];
		}

		public static string GetWasmName(IType Type)
		{
			if (Type == null)
				return "";
			return Type.FullName;
		}

		public static string GetWasmName(IMethod Method)
		{
			if (Method == null)
				return "";
			return MemberExtensions.CombineNames(GetWasmName(Method.DeclaringType), Method.Name);
		}

		public static bool IsScalar(this IType Type)
		{
			return Type.GetIsPrimitive() || Type.GetIsPointer();
		}
	}
}

