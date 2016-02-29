using System;
using System.Collections.Concurrent;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Native;
using Flame.Compiler.Statements;
using Flame.Compiler.Visitors;
using Flame.Compiler.Variables;

namespace Flame.Wasm.Passes
{
	/// <summary>
	/// A visitor that lowers field access to pointer indirection.
	/// </summary>
	public class FieldLoweringVisitor : VariableSubstitutingVisitorBase
	{
		public FieldLoweringVisitor(IAbi Abi)
		{
			this.Abi = Abi;
			this.temps = new ConcurrentDictionary<IType, LocalVariable>();
		}

		/// <summary>
		/// Gets the ABI that is used to compute field offsets.
		/// </summary>
		public IAbi Abi { get; private set; }

		private ConcurrentDictionary<IType, LocalVariable> temps;

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

		protected override bool CanSubstituteVariable(IVariable Variable)
		{
			return Variable is FieldVariable;
		}

		protected override IVariable SubstituteVariable(IVariable Variable)
		{
			var fieldVar = (FieldVariable)Variable;
			var targetTy = fieldVar.Target.Type;
			var ptr = fieldVar.Target;
			IStatement init = EmptyStatement.Instance;
			if (targetTy.GetIsValueType())
			{
				var localCopy = temps.GetOrAdd(targetTy, ty => new LocalVariable(ty));
				init = localCopy.CreateSetStatement(ptr);
				ptr = localCopy.CreateAddressOfExpression();
			}
			int offset = Abi.GetLayout(DereferenceType(targetTy)).Members[fieldVar.Field].Offset;
			return new AtAddressVariable(
				new ReinterpretCastExpression(
					new AddExpression(
						new ReinterpretCastExpression(
							new InitializedExpression(init, ptr).Simplify(), 
							Abi.PointerIntegerType),
						new StaticCastExpression(new Int32Expression(offset), Abi.PointerIntegerType).Simplify()),
					fieldVar.Field.FieldType.MakePointerType(PointerKind.ReferencePointer)));
		}
	}
	
	/// <summary>
	/// A pass that lowers field access to pointer indirection.
	/// </summary>
	public class FieldLoweringPass : IPass<IStatement, IStatement>
	{
		public FieldLoweringPass(IAbi Abi)
		{
			this.Abi = Abi;
		}

		/// <summary>
		/// The name of the field lowering pass.
		/// </summary>
		public const string FieldLoweringPassName = "lower-field";

		/// <summary>
		/// Gets the ABI that is used to compute field offsets.
		/// </summary>
		public IAbi Abi { get; private set; }

		public IStatement Apply(IStatement Statement)
		{
			return new FieldLoweringVisitor(Abi).Visit(Statement);
		}
	}
}

