using System;

namespace Flame.Wasm.Emit
{
    /// <summary>
    /// A simple wasm code block implementation that is 
    /// based on a stored expression.
    /// </summary>
    public class ExprBlock : CodeBlock
    {
        public ExprBlock(
            WasmCodeGenerator CodeGenerator, WasmExpr Expression, 
            IType Type)
            : base(CodeGenerator)
        {
            this.expr = Expression;
            this.ty = Type;
        }

        private WasmExpr expr;
        private IType ty;

        /// <summary>
        /// Converts this wasm code block to a wasm expression.
        /// </summary>
        public override WasmExpr Expression { get { return expr; } }

        /// <summary>
        /// Gets this wasm code block's type.
        /// </summary>
        public override IType Type { get { return ty; } }
    }
}

