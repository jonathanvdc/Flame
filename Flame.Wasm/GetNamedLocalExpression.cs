using System;
using Flame.Compiler;
using Flame.Wasm.Emit;

namespace Flame.Wasm
{
	/// <summary>
	/// An expression that gets a named local. This is a wasm-specific expression.
	/// </summary>
	public class GetNamedLocalExpression : IExpression
	{
		public GetNamedLocalExpression(string Name, IType Type)
		{
			this.Name = Name;
			this.Type = Type;
		}

		public string Name { get; private set; }
		public IType Type { get; private set; }

		public bool IsConstant
		{
			get { return true; }
		}

		public IExpression Accept(INodeVisitor Visitor)
		{
			return this;
		}

		public IBoundObject Evaluate()
		{
			return null;
		}

		public IExpression Optimize()
		{
			return this;
		}

		public ICodeBlock Emit(ICodeGenerator CodeGenerator)
		{
			var wasmCodeGen = (WasmCodeGenerator)CodeGenerator;
			return wasmCodeGen.EmitCallBlock(OpCodes.GetLocal, Type, new IdentifierExpr(Name));
		}
	}
}

