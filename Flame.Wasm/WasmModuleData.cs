using System;

namespace Flame.Wasm
{
    /// <summary>
    /// A data structure that contains a wasm module's ABI 
    /// and linear memory layout.
    /// </summary>
    public sealed class WasmModuleData
    {
        public WasmModuleData(IWasmAbi Abi)
        {
            this.Abi = Abi;
            this.Memory = new MemoryLayout();
        }

        /// <summary>
        /// Gets this ABI this wasm module uses.
        /// </summary>
        public IWasmAbi Abi { get; private set; }

        /// <summary>
        /// Gets the linear memory layout for this wasm module.
        /// </summary>
        public MemoryLayout Memory { get; private set; }
    }
}

