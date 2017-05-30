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

        public override string ToString()
        {
            return Mnemonic;
        }
    }

    public static class OpCodes
    {
        #region PseudoOps

        /// <summary>
        /// The declare-entry-point pseudo-op, which can only appear in a module definition.
        /// </summary>
        public static readonly OpCode DeclareStart = new OpCode("start", ExprKind.Identifier);

        /// <summary>
        /// The declare-memory pseudo-op, which can only appear in a module definition.
        /// </summary>
        public static readonly OpCode DeclareMemory = new OpCode("memory", ExprKind.Int32, ExprKind.CallList);

        /// <summary>
        /// The declare-segment pseudo-op, which can only appear in a declare-memory call.
        /// </summary>
        public static readonly OpCode DeclareSegment = new OpCode("segment", ExprKind.Int32, ExprKind.String);

        /// <summary>
        /// The declare-import pseudo-op, which can only appear in a module definition.
        /// </summary>
        public static readonly OpCode DeclareImport = new OpCode("import", ExprKind.Identifier, ExprKind.CallList);

        /// <summary>
        /// The declare-function pseudo-op, which can only appear in a module definition.
        /// </summary>
        public static readonly OpCode DeclareFunction = new OpCode("func", ExprKind.Identifier, ExprKind.CallList);

        /// <summary>
        /// The declare-local pseudo-op, which can only appear in a function definition.
        /// </summary>
        public static readonly OpCode DeclareLocal = new OpCode("local", ExprKind.Identifier, ExprKind.Mnemonic);

        /// <summary>
        /// The declare-parameter pseudo-op, which can only appear in a function definition.
        /// </summary>
        public static readonly OpCode DeclareParameter = new OpCode("param", ExprKind.Identifier, ExprKind.Mnemonic);

        /// <summary>
        /// The declare-result pseudo-op, which can only appear in a function definition.
        /// </summary>
        public static readonly OpCode DeclareResult = new OpCode("result", ExprKind.Mnemonic);

        #endregion

        #region Constants

        public static readonly OpCode Int32Const = new OpCode("i32.const", ExprKind.Int32);
        public static readonly OpCode Int64Const = new OpCode("i64.const", ExprKind.Int64);
        public static readonly OpCode Float32Const = new OpCode("f32.const", ExprKind.Float32);
        public static readonly OpCode Float64Const = new OpCode("f64.const", ExprKind.Float64);
        public static readonly OpCode Nop = new OpCode("nop");

        #endregion

        #region Arithmetic

        #region i32

        public static readonly OpCode Int32Add = new OpCode("i32.add", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32Subtract = new OpCode("i32.sub", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32Multiply = new OpCode("i32.mul", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32DivideSigned = new OpCode("i32.div_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32DivideUnsigned = new OpCode("i32.div_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32RemainderSigned = new OpCode("i32.rem_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32RemainderUnsigned = new OpCode("i32.rem_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32And = new OpCode("i32.and", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32Or = new OpCode("i32.or", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32Xor = new OpCode("i32.xor", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32ShiftLeft = new OpCode("i32.shl", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32ShiftRightSigned = new OpCode("i32.shr_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int32ShiftRightUnsigned = new OpCode("i32.shr_u", ExprKind.Call, ExprKind.Call);
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

        #endregion

        #region i64

        public static readonly OpCode Int64Add = new OpCode("i64.add", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64Subtract = new OpCode("i64.sub", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64Multiply = new OpCode("i64.mul", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64DivideSigned = new OpCode("i64.div_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64DivideUnsigned = new OpCode("i64.div_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64RemainderSigned = new OpCode("i64.rem_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64RemainderUnsigned = new OpCode("i64.rem_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64And = new OpCode("i64.and", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64Or = new OpCode("i64.or", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64Xor = new OpCode("i64.xor", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64ShiftLeft = new OpCode("i64.shl", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64ShiftRightSigned = new OpCode("i64.shr_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64ShiftRightUnsigned = new OpCode("i64.shr_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64Equal = new OpCode("i64.eq", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64NotEqual = new OpCode("i64.ne", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64LessThanSigned = new OpCode("i64.lt_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64LessThanUnsigned = new OpCode("i64.lt_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64LessThanOrEqualSigned = new OpCode("i64.le_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64LessThanOrEqualUnsigned = new OpCode("i64.le_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64GreaterThanSigned = new OpCode("i64.gt_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64GreaterThanUnsigned = new OpCode("i64.gt_u", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64GreaterThanOrEqualSigned = new OpCode("i64.ge_s", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Int64GreaterThanOrEqualUnsigned = new OpCode("i64.ge_u", ExprKind.Call, ExprKind.Call);

        #endregion

        #region f32

        public static readonly OpCode Float32Add = new OpCode("f32.add", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32Subtract = new OpCode("f32.sub", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32Multiply = new OpCode("f32.mul", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32Divide = new OpCode("f32.div", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32Negate = new OpCode("f32.neg", ExprKind.Call);
        public static readonly OpCode Float32Equal = new OpCode("f32.eq", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32NotEqual = new OpCode("f32.ne", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32LessThan = new OpCode("f32.lt", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32LessThanOrEqual = new OpCode("f32.le", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32GreaterThan = new OpCode("f32.gt", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float32GreaterThanOrEqual = new OpCode("f32.ge", ExprKind.Call, ExprKind.Call);

        #endregion

        #region f64

        public static readonly OpCode Float64Add = new OpCode("f64.add", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64Subtract = new OpCode("f64.sub", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64Multiply = new OpCode("f64.mul", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64Divide = new OpCode("f64.div", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64Negate = new OpCode("f64.neg", ExprKind.Call);
        public static readonly OpCode Float64Equal = new OpCode("f64.eq", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64NotEqual = new OpCode("f64.ne", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64LessThan = new OpCode("f64.lt", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64LessThanOrEqual = new OpCode("f64.le", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64GreaterThan = new OpCode("f64.gt", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Float64GreaterThanOrEqual = new OpCode("f64.ge", ExprKind.Call, ExprKind.Call);

        #endregion

        #endregion

        #region Conversions

        /// <summary>
        /// Wrap a 64-bit integer to a 32-bit integer.
        /// </summary>
        public static readonly OpCode Int32WrapInt64 = new OpCode("i32.wrap/i64", ExprKind.Call);

        /// <summary>
        /// Truncate a 64-bit float to a signed 32-bit integer.
        /// </summary>
        public static readonly OpCode Int32TruncateFloat64 = new OpCode("i32.trunc_s/f64", ExprKind.Call);

        /// <summary>
        /// Truncate a 32-bit float to a signed 32-bit integer.
        /// </summary>
        public static readonly OpCode Int32TruncateFloat32 = new OpCode("i32.trunc_s/f32", ExprKind.Call);

        /// <summary>
        /// Truncate a 64-bit float to a unsigned 32-bit integer.
        /// </summary>
        public static readonly OpCode UInt32TruncateFloat64 = new OpCode("i32.trunc_u/f64", ExprKind.Call);

        /// <summary>
        /// Truncate a 32-bit float to a unsigned 32-bit integer.
        /// </summary>
        public static readonly OpCode UInt32TruncateFloat32 = new OpCode("i32.trunc_u/f32", ExprKind.Call);

        /// <summary>
        /// Reinterpret the bits of a 32-bit float as a 32-bit integer.
        /// </summary>
        public static readonly OpCode Int32ReinterpretFloat32 = new OpCode("i32.reinterpret/f32", ExprKind.Call);

        /// <summary>
        /// Extend a signed 32-bit integer to a 64-bit integer.
        /// </summary>
        public static readonly OpCode Int64ExtendInt32 = new OpCode("i64.extend_s/i32", ExprKind.Call);

        /// <summary>
        /// Extend an unsigned 32-bit integer to a 64-bit integer.
        /// </summary>
        public static readonly OpCode Int64ExtendUInt32 = new OpCode("i64.extend_u/i32", ExprKind.Call);

        /// <summary>
        /// Truncate a 32-bit float to a signed 64-bit integer.
        /// </summary>
        public static readonly OpCode Int64TruncateFloat32 = new OpCode("i64.trunc_s/f32", ExprKind.Call);

        /// <summary>
        /// Truncate a 32-bit float to a signed 64-bit integer.
        /// </summary>
        public static readonly OpCode Int64TruncateFloat64 = new OpCode("i64.trunc_s/f64", ExprKind.Call);

        /// <summary>
        /// Truncate a 32-bit float to an unsigned 64-bit integer.
        /// </summary>
        public static readonly OpCode UInt64TruncateFloat32 = new OpCode("i64.trunc_u/f32", ExprKind.Call);

        /// <summary>
        /// Truncate a 32-bit float to an unsigned 64-bit integer.
        /// </summary>
        public static readonly OpCode UInt64TruncateFloat64 = new OpCode("i64.trunc_u/f64", ExprKind.Call);

        /// <summary>
        /// Reinterpret the bits of a 64-bit float as a 64-bit integer.
        /// </summary>
        public static readonly OpCode Int64ReinterpretFloat64 = new OpCode("i64.reinterpret/f64", ExprKind.Call);

        /// <summary>
        /// Demote a 64-bit float to a 32-bit float.
        /// </summary>
        public static readonly OpCode Float32DemoteFloat64 = new OpCode("f32.demote/f64", ExprKind.Call);

        /// <summary>
        /// Convert a signed 32-bit integer to a 32-bit float.
        /// </summary>
        public static readonly OpCode Float32ConvertInt32 = new OpCode("f32.convert_s/i32", ExprKind.Call);

        /// <summary>
        /// Convert a signed 64-bit integer to a 32-bit float.
        /// </summary>
        public static readonly OpCode Float32ConvertInt64 = new OpCode("f32.convert_s/i64", ExprKind.Call);

        /// <summary>
        /// Convert an unsigned 32-bit integer to a 32-bit float.
        /// </summary>
        public static readonly OpCode Float32ConvertUInt32 = new OpCode("f32.convert_u/i32", ExprKind.Call);

        /// <summary>
        /// Convert an unsigned 64-bit integer to a 32-bit float.
        /// </summary>
        public static readonly OpCode Float32ConvertUInt64 = new OpCode("f32.convert_u/i64", ExprKind.Call);

        /// <summary>
        /// Reinterpret the bits of a 32-bit integer as a 32-bit float.
        /// </summary>
        public static readonly OpCode Float32ReinterpretInt32 = new OpCode("f32.reinterpret/i32", ExprKind.Call);

        /// <summary>
        /// Promote a 32-bit float to a 64-bit float.
        /// </summary>
        public static readonly OpCode Float64PromoteFloat32 = new OpCode("f64.promote/f32", ExprKind.Call);

        /// <summary>
        /// Convert a signed 32-bit integer to a 64-bit float.
        /// </summary>
        public static readonly OpCode Float64ConvertInt32 = new OpCode("f64.convert_s/i32", ExprKind.Call);

        /// <summary>
        /// Convert a signed 64-bit integer to a 64-bit float.
        /// </summary>
        public static readonly OpCode Float64ConvertInt64 = new OpCode("f64.convert_s/i64", ExprKind.Call);

        /// <summary>
        /// Convert an unsigned 32-bit integer to a 64-bit float.
        /// </summary>
        public static readonly OpCode Float64ConvertUInt32 = new OpCode("f64.convert_u/i32", ExprKind.Call);

        /// <summary>
        /// Convert an unsigned 64-bit integer to a 64-bit float.
        /// </summary>
        public static readonly OpCode Float64ConvertUInt64 = new OpCode("f64.convert_u/i64", ExprKind.Call);

        /// <summary>
        /// Reinterpret the bits of a 64-bit integer as a 64-bit float.
        /// </summary>
        public static readonly OpCode Float64ReinterpretInt64 = new OpCode("f64.reinterpret/i64", ExprKind.Call);

        #endregion

        #region Methods

        public static readonly OpCode Call = new OpCode("call", ExprKind.Identifier, ExprKind.CallList);
        public static readonly OpCode CallImport = new OpCode("call_import", ExprKind.Identifier, ExprKind.CallList);

        #endregion

        #region Control flow

        public static readonly OpCode Br = new OpCode("br", ExprKind.Identifier);
        public static readonly OpCode BrIf = new OpCode("br_if", ExprKind.Identifier, ExprKind.Call);
        public static readonly OpCode If = new OpCode("if", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode IfElse = new OpCode("if_else", ExprKind.Call, ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Block = new OpCode("block", ExprKind.Call, ExprKind.Call);
        public static readonly OpCode Loop = new OpCode("loop", ExprKind.Identifier, ExprKind.Identifier, ExprKind.Call);
        public static readonly OpCode Return = new OpCode("return", ExprKind.CallList);

        #endregion

        #region Locals

        public static readonly OpCode GetLocal = new OpCode("get_local", ExprKind.Identifier);
        public static readonly OpCode SetLocal = new OpCode("set_local", ExprKind.Identifier, ExprKind.Call);

        #endregion

        #region Memory

        public static readonly OpCode LoadInt8 = new OpCode("i32.load8_s", ExprKind.Call);
        public static readonly OpCode LoadUInt8 = new OpCode("i32.load8_u", ExprKind.Call);
        public static readonly OpCode LoadInt16 = new OpCode("i32.load16_s", ExprKind.Call);
        public static readonly OpCode LoadUInt16 = new OpCode("i32.load16_u", ExprKind.Call);
        public static readonly OpCode LoadInt32 = new OpCode("i32.load", ExprKind.Call);
        public static readonly OpCode LoadInt64 = new OpCode("i64.load", ExprKind.Call);
        public static readonly OpCode LoadFloat32 = new OpCode("f32.load", ExprKind.Call);
        public static readonly OpCode LoadFloat64 = new OpCode("f64.load", ExprKind.Call);

        public static readonly OpCode StoreInt8 = new OpCode("i32.store8", ExprKind.Call); 
        public static readonly OpCode StoreInt16 = new OpCode("i32.store16", ExprKind.Call);
        public static readonly OpCode StoreInt32 = new OpCode("i32.store", ExprKind.Call);
        public static readonly OpCode StoreInt64 = new OpCode("i64.store", ExprKind.Call);
        public static readonly OpCode StoreFloat32 = new OpCode("f32.store", ExprKind.Call);
        public static readonly OpCode StoreFloat64 = new OpCode("f64.store", ExprKind.Call);

        #endregion
    }
}

