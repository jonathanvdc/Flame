using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Wasm.Emit;
using Flame.Compiler.Emit;
using Wasm;
using Flame.Compiler;

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
        /// Creates a memory layout for a WebAssembly module based on the given
        /// compiler options.
        /// </summary>
        /// <param name="Options">The compiler options.</param>
        /// <returns>A memory layout.</returns>
        MemoryLayoutBuilder CreateMemoryLayout(ICompilerOptions Options);

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

