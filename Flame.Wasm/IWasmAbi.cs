using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Wasm.Emit;
using Flame.Compiler.Emit;

namespace Flame.Wasm
{
    /// <summary>
    /// An ABI interface for callable wasm functions.
    /// </summary>
    public interface IWasmCallAbi : ICallAbi
    {
        /// <summary>
        /// Gets the given method's signature, as a sequence of
        /// 'param' and 'result' expressions.
        /// </summary>
        IEnumerable<WasmExpr> GetSignature(IMethod Method);
    }

    /// <summary>
	/// An ABI interface for wasm applications.
	/// </summary>
    public interface IWasmAbi : IStackAbi, IWasmCallAbi
	{
        /// <summary>
        /// Gets the ABI that is used for module imports.
        /// </summary>
        IWasmCallAbi ImportAbi { get; }

        /// <summary>
        /// Initializes the given wasm module's memory layout.
        /// </summary>
        void InitializeMemory(WasmModule Module);

        /// <summary>
        /// Create a number of wasm expressions that setup the
        /// given wasm module's entry point.
        /// </summary>
        IEnumerable<WasmExpr> SetupEntryPoint(WasmModule Module);

		/// <summary>
		/// Gets the 'this' pointer.
		/// </summary>
		IEmitVariable GetThisPointer(WasmCodeGenerator CodeGenerator);
	}
}

