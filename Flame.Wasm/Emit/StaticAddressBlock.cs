using System;
using Flame.Compiler;
using Wasm.Instructions;

namespace Flame.Wasm.Emit
{
    /// <summary>
    /// A WebAssembly block that loads a static memory address.
    /// </summary>
    public sealed class StaticAddressBlock : CodeBlock
    {
        public StaticAddressBlock(
            WasmCodeGenerator CodeGenerator,
            UniqueTag MemoryChunkTag, 
            IType Type)
            : base(CodeGenerator)
        {
            this.MemoryChunkTag = MemoryChunkTag;
            this.ty = Type;
        }

        /// <summary>
        /// Gets the tag of the memory chunk whose address is to be loaded.
        /// </summary>
        /// <returns>The tag of a memory chunk.</returns>
        public UniqueTag MemoryChunkTag { get; private set; }

        private IType ty;

        /// <inheritdoc/>
        public override IType Type { get { return ty; } }

        /// <inheritdoc/>
        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            var instr = Operators.Int32Const.Create(File.Memory.ChunkOffsets[MemoryChunkTag]);
            return new WasmExpr(instr);
        }
    }
}