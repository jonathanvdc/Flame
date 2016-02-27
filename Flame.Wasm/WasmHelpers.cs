using System;

namespace Flame.Wasm
{
	public static class WasmHelpers
	{
		public static string MangleName(IType Type)
		{
			if (Type == null)
				return "";
			return Type.FullName;
		}

		public static string MangleName(IMethod Method)
		{
			if (Method == null)
				return "";
			return MemberExtensions.CombineNames(MangleName(Method.DeclaringType), Method.Name);
		}

		public static bool IsScalar(this IType Type)
		{
			return Type.GetIsPrimitive() || Type.GetIsPointer();
		}
	}
}

