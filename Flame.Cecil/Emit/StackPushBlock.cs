using System;
using Mono.Cecil.Cil;
using Flame.Compiler;

namespace Flame.Cecil.Emit
{
	public class StackPushBlock : ICecilBlock
	{
		public StackPushBlock(ICodeGenerator CodeGenerator, ICecilBlock Value)
		{
			this.CodeGenerator = CodeGenerator;
			this.Value = Value;
		}

		public ICodeGenerator CodeGenerator { get; private set; }
		public ICecilBlock Value { get; private set; }

		public IType BlockType { get { return PrimitiveTypes.Void; } }

		public void Emit(IEmitContext Context)
		{
			// Emit the value expression, and "pop" its
			// value from the type stack, because stackpush/
			// stackpeek/stackpop can reach across control flow
			// boundaries.

			Value.Emit(Context);
			Context.Stack.Pop();
		}
	}

	public class StackPeekBlock : ICecilBlock
	{		
		public StackPeekBlock(ICodeGenerator CodeGenerator, IType BlockType)
		{
			this.CodeGenerator = CodeGenerator;
			this.BlockType = BlockType;
		}

		public ICodeGenerator CodeGenerator { get; private set; }
		public IType BlockType { get; private set; }

		public void Emit(IEmitContext Context)
		{
			// Emit a dup opcode, and "push" a type
			// onto the type stack, because stackpush/
			// stackpeek/stackpop can reach across control flow
			// boundaries.

			Context.Emit(OpCodes.Dup);
			Context.Stack.Push(BlockType);
		}
	}

	public class StackPopBlock : ICecilBlock
	{		
		public StackPopBlock(ICodeGenerator CodeGenerator, IType BlockType)
		{
			this.CodeGenerator = CodeGenerator;
			this.BlockType = BlockType;
		}

		public ICodeGenerator CodeGenerator { get; private set; }
		public IType BlockType { get; private set; }

		public void Emit(IEmitContext Context)
		{
			// Do nothing, and "push" a type
			// onto the type stack, because stackpush/
			// stackpeek/stackpop can reach across control flow
			// boundaries.

			Context.Stack.Push(BlockType);
		}
	}
}

