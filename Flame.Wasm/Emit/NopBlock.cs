using System;

namespace Flame.Wasm.Emit
{
	public class NopBlock : CodeBlock
	{
		public NopBlock(WasmCodeGenerator CodeGenerator)
			: base(CodeGenerator)
		{ }

		/// <summary>
		/// Converts this wasm code block to a wasm expression.
		/// </summary>
		public override WasmExpr Expression { get { return new CallExpr(OpCodes.Nop); } }

		/// <summary>
		/// Gets this wasm code block's type.
		/// </summary>
		public override IType Type { get { return PrimitiveTypes.Void; } }
	}
}

