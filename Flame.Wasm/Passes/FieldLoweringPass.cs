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

		protected override bool CanSubstituteVariable(IVariable Variable)
		{
			return Variable is FieldVariable;
		}

		protected override IVariable SubstituteVariable(IVariable Variable)
		{
			var fieldVar = (FieldVariable)Variable;
            if (fieldVar.Target == null)
                return fieldVar;
            
			var targetTy = fieldVar.Target.Type;
			var ptr = fieldVar.Target;
			IStatement init = EmptyStatement.Instance;
			if (targetTy.GetIsValueType())
			{
				var localCopy = temps.GetOrAdd(targetTy, ty => new LocalVariable(ty));
				init = localCopy.CreateSetStatement(ptr);
				ptr = localCopy.CreateAddressOfExpression();
			}
			int offset = Abi.GetLayout(CopyLoweringPass.DereferenceType(targetTy)).Members[fieldVar.Field].Offset;
            return new AtAddressVariable(
                CopyLoweringPass.IndexPointer(
                    new InitializedExpression(init, ptr).Simplify(), 
                    offset, Abi, fieldVar.Field.FieldType));
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

