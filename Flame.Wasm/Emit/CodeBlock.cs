using System;
using Flame.Compiler;

namespace Flame.Wasm.Emit
{
	/// <summary>
	/// A base class for wasm code blocks.
	/// </summary>
	public abstract class CodeBlock : ICodeBlock
	{
		public CodeBlock(WasmCodeGenerator CodeGenerator)
		{
			this.CodeGenerator = CodeGenerator;
		}

		/// <summary>
		/// Gets the code generator that created this code block.
		/// </summary>
		public WasmCodeGenerator CodeGenerator { get; private set; }

		ICodeGenerator ICodeBlock.CodeGenerator
		{
			get { return CodeGenerator; }
		}

		/// <summary>
		/// Gets this wasm code block's type.
		/// </summary>
		public abstract IType Type { get; }

		/// <summary>
		/// Converts this wasm code block to a wasm expression.
		/// </summary>
		public abstract WasmExpr Expression { get; }

		/// <summary>
		/// Converts the given block to a wasm expression.
		/// </summary>
		public static WasmExpr ToExpression(ICodeBlock Block)
		{
			return ((CodeBlock)Block).Expression;
		}

		public override string ToString()
		{
			return Expression.ToString();
		}
	}
}

