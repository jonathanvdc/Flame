using System;
using Wasm.Instructions;

namespace Flame.Wasm.Emit
{
    /// <summary>
    /// A simple WebAssembly block implementation that concatenates two blocks.
    /// </summary>
    public sealed class SequenceBlock : CodeBlock
    {
        public SequenceBlock(
            WasmCodeGenerator CodeGenerator,
            CodeBlock First,
            CodeBlock Second)
            : base(CodeGenerator)
        {
            this.First = First;
            this.Second = Second;
            this.ty = First.Type.Equals(PrimitiveTypes.Void) ? Second.Type : First.Type;
        }

        /// <summary>
        /// Gets the first block in the sequence.
        /// </summary>
        /// <returns>The first block in the sequence.</returns>
        public CodeBlock First { get; private set; }

        /// <summary>
        /// Gets the second block in the sequence.
        /// </summary>
        /// <returns>The second block in the sequence.</returns>
        public CodeBlock Second { get; private set; }

        private IType ty;

        /// <inheritdoc/>
        public override IType Type { get { return ty; } }

        /// <inheritdoc/>
        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            return First.ToExpression(Context, File)
                .Concat(Second.ToExpression(Context, File));
        }
    }
}

