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

		public static string GetScalarWasmName(IType Type, IWasmAbi Abi)
		{
			if (Type.GetIsPointer())
				return scalarWasmNames[Abi.PointerIntegerType];
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

		/// <summary>
		/// Creates a parameter declaration expression for the given parameter.
		/// </summary>
		public static WasmExpr DeclareParameter(IParameter Parameter, IWasmAbi Abi)
		{
			return new CallExpr(OpCodes.DeclareParameter, new IdentifierExpr(Parameter.Name), new MnemonicExpr(GetScalarWasmName(Parameter.ParameterType, Abi)));
		}

		/// <summary>
		/// Creates a result declaration expression for the given return type.
		/// </summary>
		public static WasmExpr DeclareResult(IType Type, IWasmAbi Abi)
		{
			return new CallExpr(OpCodes.DeclareResult, new MnemonicExpr(GetScalarWasmName(Type, Abi)));
		}
	}
}

