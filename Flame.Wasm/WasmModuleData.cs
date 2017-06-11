using System;
using Flame.Compiler;
using Flame.Compiler.Native;

namespace Flame.Wasm
{
    /// <summary>
    /// A data structure that contains a WebAssembly module's ABI 
    /// and linear memory layout.
    /// </summary>
    public sealed class WasmModuleData
    {
        /// <summary>
        /// Creates WebAssembly module data from the given ABI and
        /// memory layout.
        /// </summary>
        /// <param name="Abi">The WebAssembly ABI.</param>
        /// <param name="Memory">The (initialized) memory layout.</param>
        public WasmModuleData(IWasmAbi Abi, MemoryLayoutBuilder Memory)
        {
            this.Abi = Abi;
            this.Memory = Memory;
        }

        /// <summary>
        /// Gets this ABI this wasm module uses.
        /// </summary>
        public IWasmAbi Abi { get; private set; }

        /// <summary>
        /// Gets the linear memory layout for this wasm module.
        /// </summary>
        public MemoryLayoutBuilder Memory { get; private set; }

        /// <summary>
        /// Gets the global memory section of this WebAssembly module.
        /// </summary>
        /// <returns>The module's global section.</returns>
        public MemorySectionBuilder GlobalSection => Memory.GetSection(GlobalSectionName);

        /// <summary>
        /// The name of the heap memory section.
        /// </summary>
        public const string HeapSectionName = "heap";

        /// <summary>
        /// The name of the stack memory section.
        /// </summary>
        public const string StackSectionName = "stack";

        /// <summary>
        /// The name of the global memory section.
        /// </summary>
        public const string GlobalSectionName = "global";

        /// <summary>
        /// Creates WebAssembly module data from the given ABI and options.
        /// </summary>
        /// <param name="Abi">The module's ABI.</param>
        /// <param name="Options">The module's options.</param>
        /// <returns>WebAssembly module data.</returns>
        public static WasmModuleData Create(IWasmAbi Abi, ICompilerOptions Options)
        {
            return new WasmModuleData(Abi, Abi.CreateMemoryLayout(Options));
        }
    }
}

