using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Wasm.Emit;

namespace Flame.Wasm
{
	/// <summary>
	/// A wasm-specific direct call expression.
	/// </summary>
	public class DirectCallExpression : IExpression, IMemberNode
	{
		public DirectCallExpression(IMethod Target, IEnumerable<IExpression> Arguments)
		{
			this.Target = Target;
			this.Arguments = Arguments;
		}

		public IMethod Target { get; private set; }
		public IEnumerable<IExpression> Arguments { get; private set; }

		public bool IsConstant
		{
			get { return Target.GetIsConstant() && Arguments.All(item => item.IsConstant); }
		}

		public IType Type
		{
			get { return Target.ReturnType; }
		}

		public IExpression Accept(INodeVisitor Visitor)
		{
			return new DirectCallExpression(Target, Arguments.Select(Visitor.Visit).ToArray());
		}

		public IMemberNode ConvertMembers(MemberConverter Converter)
		{
			var convMethod = Converter.Convert(Target);
			if (object.ReferenceEquals(Target, convMethod))
			{
				return this;
			}
			else
			{
				return new DirectCallExpression(convMethod, Arguments);
			}
		}

		public IBoundObject Evaluate()
		{
			return null;
		}

		public IExpression Optimize()
		{
			return new DirectCallExpression(Target, Arguments.OptimizeAll());
		}

		public ICodeBlock Emit(ICodeGenerator CodeGenerator)
		{
			var wasmCg = (WasmCodeGenerator)CodeGenerator;
			return wasmCg.EmitCallBlock(
				OpCodes.Call, Type,
				new WasmExpr[] { new IdentifierExpr(WasmHelpers.GetWasmName(Target)) }
					.Concat(Arguments.EmitAll(CodeGenerator).Select(CodeBlock.ToExpression))
					.ToArray());
		}
	}
}

