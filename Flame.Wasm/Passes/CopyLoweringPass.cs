using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Flame.Compiler.Visitors;
using Flame.Compiler;
using Flame.Compiler.Native;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;

namespace Flame.Wasm.Passes
{
	/// <summary>
	/// A pass that lowers aggregate copies to scalar operations.
	/// </summary>
	public class CopyLoweringVisitor : NodeVisitorBase
	{
		public CopyLoweringVisitor(IAbi Abi)
		{
			this.Abi = Abi;
			this.srcTemp = new RegisterVariable(Abi.PointerIntegerType);
			this.destTemp = new RegisterVariable(Abi.PointerIntegerType);
		}

		/// <summary>
		/// Gets the ABI this copy lowering visitor uses.
		/// </summary>
		public IAbi Abi { get; private set; }

		private RegisterVariable srcTemp;
		private RegisterVariable destTemp;

		public override bool Matches(IExpression Value)
		{
			return false;
		}

		public override bool Matches(IStatement Value)
		{
			return Value is ISetVariableNode;
		}

		protected override IExpression Transform(IExpression Expression)
		{
			return Expression;
		}

		private static IExpression GetSourcePointer(IExpression Expression)
		{
			if (Expression is IVariableNode)
			{
				var varNode = (IVariableNode)Expression;
				var srcVar = varNode.GetVariable() as IUnmanagedVariable;
				if (varNode.Action == VariableNodeAction.Get && srcVar != null)
				{
					return srcVar.CreateAddressOfExpression();
				}
			}
			else if (Expression is IMetadataNode<IExpression>)
			{
				var metaNode = (IMetadataNode<IExpression>)Expression;
				return metaNode.Value;
			}
			else if (Expression is InitializedExpression)
			{
				var initExpr = (InitializedExpression)Expression;
				return new InitializedExpression(
					initExpr.Initialization, 
					GetSourcePointer(initExpr.Value), 
					initExpr.Finalization);
			}
			throw new Exception(
				"Could not find a source pointer " +
				"for an aggregate copy operation.");
		}

		protected override IStatement Transform(IStatement Statement)
		{
			var setVarNode = (ISetVariableNode)Statement;
			var targetVar = setVarNode.GetVariable();

			var targetTy = targetVar.Type;
			if (targetTy.IsScalar())
				return Statement.Accept(this);

			var stmts = new List<IStatement>();
			
			stmts.Add(destTemp.CreateSetStatement(
				new ReinterpretCastExpression(
					Visit(((IUnmanagedVariable)targetVar).CreateAddressOfExpression()), 
					Abi.PointerIntegerType).Simplify()));

			stmts.Add(srcTemp.CreateSetStatement(
				new ReinterpretCastExpression(
					GetSourcePointer(Visit(setVarNode.Value)), 
					Abi.PointerIntegerType).Simplify()));
			
			stmts.Add(CopyLoweringPass.CreateBitwiseCopy(
				srcTemp.CreateGetExpression(), destTemp.CreateGetExpression(), 
				Abi.GetLayout(targetTy), Abi));

			return new BlockStatement(stmts).Simplify();
		}
	}

	/// <summary>
	/// A pass that lowers aggregate copies to scalar operations.
	/// </summary>
	public class CopyLoweringPass : IPass<IStatement, IStatement>
	{
		public CopyLoweringPass(IAbi Abi)
		{
			this.Abi = Abi;
		}

		/// <summary>
		/// Gets the ABI this copy lowering pass uses.
		/// </summary>
		public IAbi Abi { get; private set; }

		/// <summary>
		/// The name of the copy lowering pass.
		/// </summary>
		public const string CopyLoweringPassName = "lower-copy";

		public IStatement Apply(IStatement Value)
		{
			return new CopyLoweringVisitor(Abi).Visit(Value);
		}

		/// <summary>
		/// 'Dereferences' the given type: a single layer of 
		/// type-level pointer indirection is performed, but
		/// only if that can actually be done.
		/// </summary>
		public static IType DereferenceType(IType Type)
		{
			if (Type.GetIsPointer())
				return Type.AsPointerType().ElementType;
			else
				return Type;
		}

		/// <summary>
		/// Indexes the given base pointer at the given offset, and reinterprets the result
		/// as a pointer of the given type.
		/// </summary>
		public static IExpression IndexPointer(IExpression BasePointer, int Offset, IAbi Abi, IType Type)
		{
			return new ReinterpretCastExpression(
				new AddExpression(
					new ReinterpretCastExpression(BasePointer, Abi.PointerIntegerType).Simplify(),
					new StaticCastExpression(new Int32Expression(Offset), Abi.PointerIntegerType).Simplify()),
				Type.MakePointerType(PointerKind.ReferencePointer));
		}

		/// <summary>
		/// Creates a statement that performs a bitwise copy
		/// of the data in the source memory location to the
		/// destination memory location.
		/// </summary>
		public static IStatement CreateBitwiseCopy(
			IExpression SourcePointer, 
			IExpression DestinationPointer,
			DataLayout Layout, IAbi Abi)
		{
			var instrs = new List<IStatement>();

			int wordSize = Abi.GetLayout(Abi.PointerIntegerType).Size;

			int wordCount = Layout.Size / wordSize;
			int totalWordSize = wordCount * wordSize;
			for (int i = 0; i < wordCount; i++)
			{
				instrs.Add(
					new StoreAtAddressStatement(
						IndexPointer(SourcePointer, i * wordSize, Abi, Abi.PointerIntegerType),
						new DereferencePointerExpression(
							IndexPointer(DestinationPointer, i * wordSize, Abi, Abi.PointerIntegerType))));
			}
			for (int i = totalWordSize; i < Layout.Size; i++)
			{
				instrs.Add(
					new StoreAtAddressStatement(
						IndexPointer(SourcePointer, i, Abi, PrimitiveTypes.Bit8),
						new DereferencePointerExpression(
							IndexPointer(DestinationPointer, i, Abi, PrimitiveTypes.Bit8))));
			}
			return new BlockStatement(instrs).Simplify();
		}
	}
}

