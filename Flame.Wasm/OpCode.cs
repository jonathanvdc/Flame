using System;
using System.Collections.Generic;

namespace Flame.Wasm
{
	/// <summary>
	/// A class for Wasm opcodes.
	/// </summary>
	public sealed class OpCode
	{
		public OpCode(string Mnemonic, IReadOnlyList<ExprKind> ArgTypes)
		{
			this.Mnemonic = Mnemonic;
			this.ArgTypes = ArgTypes;
		}
		public OpCode(string Mnemonic, params ExprKind[] ArgTypes)
			: this(Mnemonic, (IReadOnlyList<ExprKind>)ArgTypes)
		{ }

		/// <summary>
		/// Gets this opcode's mnemonic.
		/// </summary>
		public string Mnemonic { get; private set; }

		/// <summary>
		/// Gets this opcode's expected argument types.
		/// </summary>
		public IReadOnlyList<ExprKind> ArgTypes { get; private set; }
	}

	public static class OpCodes
	{
		#region Constants

		public static readonly OpCode Int32Const = new OpCode("i32.const", ExprKind.Int32);
		public static readonly OpCode Int64Const = new OpCode("i64.const", ExprKind.Int64);
		public static readonly OpCode Float32Const = new OpCode("f32.const", ExprKind.Float32);
		public static readonly OpCode Float64Const = new OpCode("f64.const", ExprKind.Float64);
		public static readonly OpCode Nop = new OpCode("nop");

		#endregion

		#region Intrinsics

		public static readonly OpCode Int32Add = new OpCode("i32.add", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32Subtract = new OpCode("i32.sub", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32Multiply = new OpCode("i32.mul", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32DivideSigned = new OpCode("i32.div_s", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32DivideUnsigned = new OpCode("i32.div_u", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32Equal = new OpCode("i32.eq", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32NotEqual = new OpCode("i32.ne", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32LessThanSigned = new OpCode("i32.lt_s", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32LessThanUnsigned = new OpCode("i32.lt_u", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32LessThanOrEqualSigned = new OpCode("i32.le_s", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32LessThanOrEqualUnsigned = new OpCode("i32.le_u", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32GreaterThanSigned = new OpCode("i32.gt_s", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32GreaterThanUnsigned = new OpCode("i32.gt_u", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32GreaterThanOrEqualSigned = new OpCode("i32.ge_s", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int32GreaterThanOrEqualUnsigned = new OpCode("i32.ge_u", ExprKind.Call, ExprKind.Call);

		public static readonly OpCode Int64Add = new OpCode("i64.add", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Int64Multiply = new OpCode("i64.mul", ExprKind.Call, ExprKind.Call);

		#endregion

		#region Conversions

		public static readonly OpCode Int32WrapInt64 = new OpCode("i32.wrap/i64", ExprKind.Call);
		public static readonly OpCode Int64ExtendInt32 = new OpCode("i64.extend_s/i32", ExprKind.Call);
		public static readonly OpCode Int64ExtendUInt32 = new OpCode("i64.extend_u/i32", ExprKind.Call);

		#endregion

		#region Methods

		public static readonly OpCode Call = new OpCode("call", ExprKind.Identifier, ExprKind.CallList);

		#endregion

		#region Control flow

		public static readonly OpCode Br = new OpCode("br", ExprKind.Identifier);
		public static readonly OpCode BrIf = new OpCode("br_if", ExprKind.Identifier, ExprKind.Call);
		public static readonly OpCode If = new OpCode("if", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode IfElse = new OpCode("if_else", ExprKind.Call, ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Block = new OpCode("block", ExprKind.Call, ExprKind.Call);
		public static readonly OpCode Loop = new OpCode("loop", ExprKind.Identifier, ExprKind.Identifier, ExprKind.Call);
		public static readonly OpCode Return = new OpCode("return", ExprKind.Call);

		#endregion

		#region Locals

		public static readonly OpCode GetLocal = new OpCode("get_local", ExprKind.Identifier);
		public static readonly OpCode SetLocal = new OpCode("set_local", ExprKind.Identifier, ExprKind.Call);

		#endregion
	}
}

