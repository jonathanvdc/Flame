using System;

namespace Flame.Wasm.Emit
{
	public class DelegateBlock : CodeBlock
	{
		public DelegateBlock(
			WasmCodeGenerator CodeGenerator, CodeBlock Target, 
			IMethod Method, Operator Op)
			: base(CodeGenerator)
		{
			this.Target = Target;
			this.Method = Method;
			this.Op = Op;
		}

		public CodeBlock Target { get; private set; }
		public IMethod Method { get; private set; }
		public Operator Op { get; private set; }

		/// <summary>
		/// Gets this wasm code block's type.
		/// </summary>
		public override IType Type { get { return MethodType.Create(Method); } }

		/// <summary>
		/// Converts this wasm code block to a wasm expression.
		/// </summary>
		public override WasmExpr Expression
		{ 
			get
			{ 
				throw new NotImplementedException();
			}
		}
	}
}

