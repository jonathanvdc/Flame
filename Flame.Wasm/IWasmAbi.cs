using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Wasm.Emit;
using Flame.Compiler.Emit;

namespace Flame.Wasm
{
	/// <summary>
	/// An ABI interface for wasm applications.
	/// </summary>
	public interface IWasmAbi : IStackAbi
	{
		/// <summary>
		/// Gets the 'this' pointer.
		/// </summary>
		IEmitVariable GetThisPointer(WasmCodeGenerator CodeGenerator);

		/// <summary>
		/// Gets the given method's signature, as a sequence of
		/// 'param' and 'result' expressions.
		/// </summary>
		IEnumerable<WasmExpr> GetSignature(IMethod Method);
	}
}

