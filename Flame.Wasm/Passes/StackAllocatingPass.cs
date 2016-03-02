using System;
using System.Collections.Generic;
using Flame.Compiler;
using Flame.Compiler.Native;
using Flame.Compiler.Visitors;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Flame.Compiler.Expressions;

namespace Flame.Wasm.Passes
{
	/// <summary>
	/// A visitor that stack-allocates local variables.
	/// </summary>
	public class StackAllocatingVisitor : VariableSubstitutingVisitorBase
	{
		public StackAllocatingVisitor(IStackAbi Abi, ArgumentLayout Arguments)
		{
			this.Abi = Abi;
			this.Arguments = Arguments;
			this.StackSize = 0;
			this.variableMapping = new Dictionary<LocalVariableBase, IVariable>();
		}

		/// <summary>
		/// Gets the ABI that this visitor uses to stack-allocate 
		/// locals. 
		/// </summary>
		public IStackAbi Abi { get; private set; }

		/// <summary>
		/// Gets the argument layout for the current method.
		/// </summary>
		public ArgumentLayout Arguments { get; private set; }

		/// <summary>
		/// Gets the size of the local value stack.
		/// </summary>
		public int StackSize { get; private set; }

		private Dictionary<LocalVariableBase, IVariable> variableMapping;

		private IVariable Allocate(LocalVariableBase Variable)
		{
			var ty = Variable.Type;
			if (!(Variable is IUnmanagedVariable) && ty.IsScalar())
			{
				return Variable;
			}
			else
			{
				var result = new AtAddressVariable(
					new ReinterpretCastExpression(
						Abi.GetStackSlotAddress(new Int32Expression(StackSize)), 
						ty.MakePointerType(PointerKind.ReferencePointer)));
				var layout = Abi.GetLayout(ty);
				StackSize += layout.Size;
				return result;
			}
		}

		protected override bool CanSubstituteVariable(IVariable Variable)
		{
			return Variable is LocalVariableBase 
				|| Variable is ThisVariable 
				|| Variable is ArgumentVariable;
		}

		protected override IVariable SubstituteVariable(IVariable Variable)
		{
			if (Variable is ThisVariable)
			{
				return Arguments.ThisPointer;
			}
			else if (Variable is ArgumentVariable)
			{
				var arg = (ArgumentVariable)Variable;
				return Arguments.GetArgument(arg.Index);
			}
			else
			{
				var localVar = (LocalVariableBase)Variable;

				IVariable result;
				if (!variableMapping.TryGetValue(localVar, out result))
				{
					result = Allocate(localVar);
					variableMapping[localVar] = result;
				}
				return result;
			}
		}
	}

	/// <summary>
	/// A pass that stack-allocates all local variables, and inserts
	/// a prologue/epilogue.
	/// </summary>
	public class StackAllocatingPass : IPass<BodyPassArgument, IStatement>
	{
		public StackAllocatingPass(IStackAbi Abi)
		{
			this.Abi = Abi;
		}

		public const string StackAllocatingPassName = "stackalloc";

		/// <summary>
		/// Gets the ABI this pass uses.
		/// </summary>
		public IStackAbi Abi { get; private set; }

		public IStatement Apply(BodyPassArgument Arg)
		{
			var resultStmts = new List<IStatement>();
			var visitor = new StackAllocatingVisitor(
				Abi, Abi.GetArgumentLayout(Arg.DeclaringMethod));

			var visitedBody = visitor.Visit(Arg.Body);
			if (visitor.StackSize > 0)
				resultStmts.Add(Abi.StackAllocate(new Int32Expression(visitor.StackSize)));
			resultStmts.Add(visitedBody);
			return new BlockStatement(resultStmts).Simplify();
		}
	}
}

