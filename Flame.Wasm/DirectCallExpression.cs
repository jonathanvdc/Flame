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
        public DirectCallExpression(OpCode CallOp, IMethod Target, IEnumerable<IExpression> Arguments)
		{
            this.CallOp = CallOp;
			this.Target = Target;
			this.Arguments = Arguments;
		}

        public OpCode CallOp { get; private set; }
		public IMethod Target { get; private set; }
		public IEnumerable<IExpression> Arguments { get; private set; }

		public bool IsConstantNode
		{
			get { return Target.GetIsConstant(); }
		}

		public IType Type
		{
			get { return Target.ReturnType; }
		}

		public IExpression Accept(INodeVisitor Visitor)
		{
			return new DirectCallExpression(CallOp, Target, Arguments.Select(Visitor.Visit).ToArray());
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
                return new DirectCallExpression(CallOp, convMethod, Arguments);
			}
		}

		public IBoundObject Evaluate()
		{
			return null;
		}

		public IExpression Optimize()
		{
            return new DirectCallExpression(CallOp, Target, Arguments.OptimizeAll());
		}

		public ICodeBlock Emit(ICodeGenerator CodeGenerator)
		{
			var wasmCg = (WasmCodeGenerator)CodeGenerator;
			return wasmCg.EmitCallBlock(
                CallOp, Type,
				new WasmExpr[] { new IdentifierExpr(WasmHelpers.GetWasmName(Target)) }
					.Concat(Arguments.EmitAll(CodeGenerator).Select(CodeBlock.ToExpression))
					.ToArray());
		}
	}
}
