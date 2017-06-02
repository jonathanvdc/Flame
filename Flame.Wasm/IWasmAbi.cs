using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Wasm.Emit;
using Flame.Compiler.Emit;
using Wasm;

namespace Flame.Wasm
{
    /// <summary>
    /// An ABI interface for callable wasm functions.
    /// </summary>
    public interface IWasmCallAbi : ICallAbi
    {
        /// <summary>
        /// Gets the given method's signature.
        /// </summary>
        FunctionType GetSignature(IMethod Method);
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
        /// Adds the given module's entry point to the given WebAssembly file builder.
        /// </summary>
        /// <param name="Module">The module from which the entry point is derived.</param>
        /// <param name="File">The WebAssembly file builder to update.</param>
        void SetupEntryPoint(WasmModule Module, WasmFileBuilder File);

        /// <summary>
        /// Gets the 'this' pointer.
        /// </summary>
        IEmitVariable GetThisPointer(WasmCodeGenerator CodeGenerator);

        /// <summary>
        /// Gets the argument variable with the given index.
        /// </summary>
        IEmitVariable GetArgument(WasmCodeGenerator CodeGenerator, int Index);
    }
}

