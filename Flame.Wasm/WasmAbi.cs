using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Native;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Flame.Wasm.Emit;

namespace Flame.Wasm
{
	/// <summary>
	/// An ABI implementation for wasm.
	/// </summary>
	public class WasmAbi : IWasmAbi
	{
		public WasmAbi(IType PointerIntegerType)
		{
			this.layoutBuilder = new WasmDataLayoutBuilder(PointerIntegerType);
			this.StackPointerParameter = new DescribedParameter(StackPointerName, PointerIntegerType);
			this.StackPointerRegister = new RegisterVariable(StackPointerRegisterName, PointerIntegerType);
			this.FramePointerRegister = new RegisterVariable(FramePointerName, PointerIntegerType);
		}

		/// <summary>
		/// The frame pointer register name.
		/// </summary>
		public const string FramePointerName = "frameptr";

		/// <summary>
		/// The name of the stack pointer register.
		/// </summary>
		public const string StackPointerRegisterName = "stackptr";

		/// <summary>
		/// The stack pointer parameter name.
		/// </summary>
		public const string StackPointerName = "stacktop";

		/// <summary>
		/// The 'this' pointer parameter name.
		/// </summary>
		public const string ThisPointerName = "this";

		/// <summary>
		/// Gets the stack pointer parameter.
		/// </summary>
		public IParameter StackPointerParameter { get; private set; }

		/// <summary>
		/// Gets the frame pointer register.
		/// </summary>
		public IVariable FramePointerRegister { get; private set; }

		/// <summary>
		/// Gets the stack pointer register.
		/// </summary>
		public IVariable StackPointerRegister { get; private set; }

		private WasmDataLayoutBuilder layoutBuilder;

		/// <summary>
		/// Gets the integer type that corresponds to a pointer.
		/// </summary>
		public IType PointerIntegerType { get { return layoutBuilder.PointerIntegerType; } }

		public DataLayout GetLayout(IType Type)
		{
			return layoutBuilder.Convert(Type);
		}

		/// <summary>
		/// Writes a prologue for the given method.
		/// </summary>
		public IStatement CreatePrologue(IMethod Method)
		{
			var results = new List<IStatement>();
			results.Add(FramePointerRegister.CreateSetStatement(new GetNamedLocalExpression(StackPointerName, PointerIntegerType)));
			results.Add(StackPointerRegister.CreateSetStatement(FramePointerRegister.CreateGetExpression()));
			return new BlockStatement(results);
		}

		/// <summary>
		/// Writes a return statement/epilogue for the given method.
		/// </summary>
		public IStatement CreateReturnEpilogue(IMethod Method, IExpression Value)
		{
			return new ReturnStatement(Value);
		}

		/// <summary>
		/// Gets a pointer to the stack slot at the given offset.
		/// </summary>
		public IExpression GetStackSlotAddress(IExpression Offset)
		{
			return new AddExpression(FramePointerRegister.CreateGetExpression(), Offset);
		}

		/// <summary>
		/// Allocates the given number of bytes on the stack.
		/// </summary>
		public IStatement StackAllocate(IExpression Size)
		{
			return StackPointerRegister.CreateSetStatement(
				new AddExpression(
					StackPointerRegister.CreateGetExpression(), 
					new StaticCastExpression(Size, PointerIntegerType).Simplify()));
		}

		/// <summary>
		/// Deallocates the given number of bytes from the stack.
		/// </summary>
		public IStatement StackRelease(IExpression Size)
		{
			return StackPointerRegister.CreateSetStatement(
				new SubtractExpression(
					StackPointerRegister.CreateGetExpression(), 
					new StaticCastExpression(Size, PointerIntegerType).Simplify()));
		}

		/// <summary>
		/// Gets the stack pointer.
		/// </summary>
		public CodeBlock GetStackPointer(WasmCodeGenerator CodeGenerator)
		{
			return CodeGenerator.EmitCallBlock(
				OpCodes.GetLocal, 
				PointerIntegerType, 
				new IdentifierExpr(StackPointerName));
		}

		/// <summary>
		/// Gets the given method's calling convention spec.
		/// </summary>
		public CallingConventionSpec GetConventionSpec(IMethod Method)
		{
			var memLocals = new List<int>();
			var regLocals = new List<int>();
			int i = 0;
			foreach (var item in Method.Parameters)
			{
				if (item.ParameterType.IsScalar())
					regLocals.Add(i);
				else
					memLocals.Add(i);
				i++;
			}
			return new CallingConventionSpec(
				!Method.IsStatic, !Method.ReturnType.IsScalar(), 
				memLocals, regLocals);
		}

		/// <summary>
		/// Gets the argument layout for the given method.
		/// </summary>
		/// <returns>The argument layout.</returns>
		/// <param name="Method">The method to inspect.</param>
		public ArgumentLayout GetArgumentLayout(IMethod Method)
		{
			// This is how we'll do the argument layout:
			// 
			//     stack_argument_1
			//     ...
			//     stack_argument_n
			//     return_value (if any)
			//
			// Note, however, that we have to reverse this, because
			// we'll be using frame pointer-relative addresses.

			var spec = GetConventionSpec(Method);

			// Start off by computing stack addresses, relative
			// to the calling function's stack pointer.
			var parameters = Method.GetParameters();
			var argOffsets = new Dictionary<int, int>();

			int stackSize = 0;
			foreach (var i in spec.StackArguments)
			{
				argOffsets[i] = stackSize;
				stackSize += GetLayout(parameters[i].ParameterType).Size;
			}
			if (spec.ReturnValueOnStack)
			{
				stackSize += GetLayout(Method.ReturnType).Size;
			}

			// Now that we know the total stack size, we can compute
			// the stack layout relative to the frame pointer.
			var memLocals = new Dictionary<int, IUnmanagedVariable>();
			foreach (var i in spec.StackArguments)
			{
				// &stack_argument_i = frame_pointer - (arg_stack_size - &caller_relative_stack_argument1)

				int offset = stackSize - argOffsets[i];
				memLocals[i] = new AtAddressVariable(
					new ReinterpretCastExpression(
						new SubtractExpression(
							new ReinterpretCastExpression(FramePointerRegister.CreateGetExpression(), PointerIntegerType).Simplify(), 
							new StaticCastExpression(new Int32Expression(offset), PointerIntegerType).Simplify()),
						parameters[i].ParameterType.MakePointerType(PointerKind.ReferencePointer)));
			}

			// Oh, yeah. And consider register arguments, too.
			var regLocals = new Dictionary<int, IVariable>();
			foreach (var i in spec.RegisterArguments)
			{
				regLocals[i] = new ArgumentVariable(parameters[i], i);
			}

			return new ArgumentLayout(new ThisVariable(Method.DeclaringType), memLocals, regLocals);
		}

		/// <summary>
		/// Gets the 'this' pointer.
		/// </summary>
		public IEmitVariable GetThisPointer(WasmCodeGenerator CodeGenerator)
		{
			return new Register(
				CodeGenerator, "this", 
				ThisVariable.GetThisType(CodeGenerator.Method.DeclaringType));
		}

		/// <summary>
		/// Gets the given method's signature, as a sequence of
		/// 'param' and 'result' expressions.
		/// </summary>
		public IEnumerable<WasmExpr> GetSignature(IMethod Method)
		{
			var results = new List<WasmExpr>();
			results.Add(WasmHelpers.DeclareParameter(StackPointerParameter, this));
			var ccSpec = GetConventionSpec(Method);
			if (ccSpec.HasThisPointer)
			{
				results.Add(WasmHelpers.DeclareParameter(new DescribedParameter(ThisPointerName, ThisVariable.GetThisType(Method.DeclaringType)), this));
			}
			var paramSigs = Method.GetParameters();
			foreach (var i in ccSpec.RegisterArguments)
			{
				results.Add(WasmHelpers.DeclareParameter(paramSigs[i], this));
			}
			if (!ccSpec.ReturnValueOnStack && !Method.ReturnType.Equals(PrimitiveTypes.Void))
			{
				results.Add(WasmHelpers.DeclareResult(Method.ReturnType, this));
			}
			return results;
		}

		/// <summary>
		/// Creates a direct call to the given method. A 'this' pointer and
		/// a sequence of arguments are given.
		/// </summary>
		public IExpression CreateDirectCall(
			IMethod Target, IExpression ThisPointer, IEnumerable<IExpression> Arguments)
		{
			var callArgs = new List<IExpression>();
			callArgs.Add(StackPointerRegister.CreateGetExpression());
			if (ThisPointer != null)
				callArgs.Add(ThisPointer);
			callArgs.AddRange(Arguments);
			return new DirectCallExpression(Target, callArgs);
		}
	}

	public class WasmDataLayoutBuilder : TypeConverterBase<DataLayout>
	{
		public WasmDataLayoutBuilder(IType PointerIntegerType)
		{
			this.PointerIntegerType = PointerIntegerType;
			this.layoutDictionary = new ConcurrentDictionary<IType, DataLayout>();
		}

		/// <summary>
		/// Gets the integer type that corresponds to a pointer.
		/// </summary>
		public IType PointerIntegerType { get; private set; }

		private ConcurrentDictionary<IType, DataLayout> layoutDictionary;

		protected override DataLayout ConvertTypeDefault(IType Type)
		{
			throw new InvalidOperationException();
		}

		protected override DataLayout MakeGenericType(DataLayout GenericDeclaration, IEnumerable<DataLayout> TypeArguments)
		{
			throw new InvalidOperationException();
		}

		protected override DataLayout MakePointerType(DataLayout ElementType, PointerKind Kind)
		{
			return Convert(PointerIntegerType);
		}

		protected override DataLayout MakeArrayType(DataLayout ElementType, int ArrayRank)
		{
			throw new NotImplementedException();
		}

		protected override DataLayout MakeVectorType(DataLayout ElementType, IReadOnlyList<int> Dimensions)
		{
			return new DataLayout(ElementType.Size * Dimensions.Aggregate(1, (result, item) => result * item));
		}

		protected override DataLayout ConvertPrimitiveType(IType Type)
		{
			return new DataLayout(Type.GetPrimitiveSize());
		}

		protected override DataLayout ConvertReferenceType(IType Type)
		{
			return Convert(PointerIntegerType);
		}

		protected override DataLayout ConvertValueType(IType Type)
		{
			var members = new Dictionary<IField, DataMember>();
			int size = 0;
			foreach (var item in Type.Fields)
			{
				if (!item.IsStatic)
				{
					var fieldMember = new DataMember(Convert(item.FieldType), size);
					members[item] = fieldMember;
					size += fieldMember.Layout.Size;
				}
			}
			return new DataLayout(size, members);
		}

		protected override DataLayout ConvertEnumType(IType Type)
		{
			return Convert(Type.GetParent() ?? PrimitiveTypes.Int32);
		}

		protected override DataLayout ConvertPointerType(IPointerType Type)
		{
			return Convert(PointerIntegerType);
		}

		public override DataLayout Convert(IType Value)
		{
			return layoutDictionary.GetOrAdd(Value, base.Convert);
		}
	}
}

