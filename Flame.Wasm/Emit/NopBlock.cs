using System;
using Wasm.Instructions;

namespace Flame.Wasm.Emit
{
    public sealed class NopBlock : CodeBlock
    {
        public NopBlock(WasmCodeGenerator CodeGenerator)
            : base(CodeGenerator)
        { }

        /// <summary>
        /// Converts this wasm code block to a wasm expression.
        /// </summary>
        public WasmExpr Expression { get { return new WasmExpr(Operators.Nop.Create()); } }

        /// <summary>
        /// Gets this wasm code block's type.
        /// </summary>
        public override IType Type { get { return PrimitiveTypes.Void; } }

        public override WasmExpr ToExpression(BlockContext Context, WasmFileBuilder File)
        {
            return Expression;
        }
    }
}

