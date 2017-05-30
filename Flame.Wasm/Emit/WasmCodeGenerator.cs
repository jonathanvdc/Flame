using System;
using System.Collections.Generic;
using System.Linq;
using Flame;
using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Variables;
using Flame.Compiler.Native;
using Flame.Compiler.Expressions;

namespace Flame.Wasm.Emit
{
    /// <summary>
    /// A code generator for wasm.
    /// </summary>
    public class WasmCodeGenerator : ICodeGenerator, IUnmanagedCodeGenerator
    {
        public WasmCodeGenerator(IMethod Method, IWasmAbi Abi)
        {
            this.Method = Method;
            this.Abi = Abi;
            this.locals = new Dictionary<UniqueTag, IEmitVariable>();
            this.registers = new List<Register>();
            var localNameSet = new UniqueNameSet<UniqueTag>(item => item.Name, "tmp");
            this.localNames = new UniqueNameMap<UniqueTag>(localNameSet);
            this.breakTags = new UniqueNameMap<UniqueTag>(new UniqueNameSet<UniqueTag>(item => item.Name, "break", localNameSet));
            this.continueTags = new UniqueNameMap<UniqueTag>(new UniqueNameSet<UniqueTag>(item => item.Name, "next", localNameSet));

            // Register reserved names
            this.localNames.Get(new UniqueTag(WasmAbi.ThisPointerName));
            this.localNames.Get(new UniqueTag(WasmAbi.StackPointerName));
            this.localNames.Get(new UniqueTag(WasmAbi.ReturnPointerName));
            foreach (var item in Method.Parameters)
            {
                this.localNames.Get(new UniqueTag(item.Name.ToString()));
            }
        }

        /// <summary>
        /// Gets the method this code generator belongs to.
        /// </summary>
        public IMethod Method { get; private set; }

        /// <summary>
        /// Gets the ABI that this function uses.
        /// </summary>
        public IWasmAbi Abi { get; private set; }

        private Dictionary<UniqueTag, IEmitVariable> locals;
        private List<Register> registers;

        private UniqueNameMap<UniqueTag> localNames;
        private UniqueNameMap<UniqueTag> breakTags;
        private UniqueNameMap<UniqueTag> continueTags;

        #region Helpers

        public CodeBlock EmitCallBlock(OpCode Target, IType Type, params WasmExpr[] Args)
        {
            return new ExprBlock(this, new CallExpr(Target, Args), Type);
        }

        #endregion

        #region Prologue/Epilogue

        /// <summary>
        /// Gets a sequence of register declaration expressions.
        /// </summary>
        public IEnumerable<WasmExpr> RegisterDeclarations
        {
            get
            {
                return registers.Select(item =>
                    new CallExpr(OpCodes.DeclareLocal,
                        new IdentifierExpr(item.Identifier),
                        new MnemonicExpr(WasmHelpers.GetScalarWasmName(item.Type, Abi))));
            }
        }

        /// <summary>
        /// Wraps the given body expression in a prologue for this
        /// function.
        /// </summary>
        public IEnumerable<WasmExpr> WrapBody(WasmExpr Body)
        {
            return RegisterDeclarations.Concat(new WasmExpr[] { Body });
        }

        #endregion

        #region Constants

        private CodeBlock EmitTypedInt32(int Value, IType Type)
        {
            return EmitCallBlock(OpCodes.Int32Const, Type, new Int32Expr(Value));
        }

        private CodeBlock EmitTypedInt64(long Value, IType Type)
        {
            return EmitCallBlock(OpCodes.Int64Const, Type, new Int64Expr(Value));
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return EmitTypedInt32(Value, PrimitiveTypes.Bit8);
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return EmitTypedInt32(Value, PrimitiveTypes.Bit16);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return EmitTypedInt32((int)Value, PrimitiveTypes.Bit32);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return EmitTypedInt64((long)Value, PrimitiveTypes.Bit64);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return EmitTypedInt32(Value, PrimitiveTypes.Int8);
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return EmitTypedInt32(Value, PrimitiveTypes.Int16);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return EmitTypedInt32(Value, PrimitiveTypes.Int32);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return EmitTypedInt64(Value, PrimitiveTypes.Int64);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return EmitTypedInt32(Value, PrimitiveTypes.UInt8);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return EmitTypedInt32(Value, PrimitiveTypes.UInt16);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return EmitTypedInt32((int)Value, PrimitiveTypes.UInt32);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return EmitTypedInt64((long)Value, PrimitiveTypes.UInt64);
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return EmitCallBlock(OpCodes.Float32Const, PrimitiveTypes.Float32, new Float32Expr(Value));
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return EmitCallBlock(OpCodes.Float64Const, PrimitiveTypes.Float64, new Float64Expr(Value));
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return EmitTypedInt32(Value ? 1 : 0, PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return EmitTypedInt32((int)Value, PrimitiveTypes.Char);
        }

        public ICodeBlock EmitNull()
        {
            return EmitTypedInt32(0, PrimitiveTypes.Null);
        }

        public ICodeBlock EmitVoid()
        {
            return new NopBlock(this);
        }

        public ICodeBlock EmitString(string Value)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitBit(BitValue Value)
        {
            int size = Value.Size;
            if (size == 8)
                return EmitBit8(Value.ToInteger().ToUInt8());
            else if (size == 16)
                return EmitBit16(Value.ToInteger().ToUInt16());
            else if (size == 32)
                return EmitBit32(Value.ToInteger().ToUInt32());
            else if (size == 64)
                return EmitBit64(Value.ToInteger().ToUInt64());
            else
                throw new NotSupportedException("Unsupported bit size: " + size);
        }

        public ICodeBlock EmitInteger(IntegerValue Value)
        {
            var spec = Value.Spec;
            if (spec.Equals(IntegerSpec.Int8))
                return EmitInt8(Value.ToInt8());
            else if (spec.Equals(IntegerSpec.Int16))
                return EmitInt16(Value.ToInt16());
            else if (spec.Equals(IntegerSpec.Int32))
                return EmitInt32(Value.ToInt32());
            else if (spec.Equals(IntegerSpec.Int64))
                return EmitInt64(Value.ToInt64());
            else if (spec.Equals(IntegerSpec.UInt8))
                return EmitUInt8(Value.ToUInt8());
            else if (spec.Equals(IntegerSpec.UInt16))
                return EmitUInt16(Value.ToUInt16());
            else if (spec.Equals(IntegerSpec.UInt32))
                return EmitUInt32(Value.ToUInt32());
            else if (spec.Equals(IntegerSpec.UInt64))
                return EmitUInt64(Value.ToUInt64());
            else
                throw new NotSupportedException("Unsupported integer spec: " + spec.ToString());
        }

        #endregion

        #region Control Flow

        public ICodeBlock EmitBreak(UniqueTag Target)
        {
            return EmitCallBlock(OpCodes.Br, PrimitiveTypes.Void, new IdentifierExpr(breakTags[Target]));
        }

        public ICodeBlock EmitContinue(UniqueTag Target)
        {
            return EmitCallBlock(OpCodes.Br, PrimitiveTypes.Void, new IdentifierExpr(continueTags[Target]));
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            var retVal = Value as CodeBlock;
            if (retVal == null)
                return EmitCallBlock(OpCodes.Return, PrimitiveTypes.Void);
            else if (retVal.Type.Equals(PrimitiveTypes.Void))
                return EmitSequence(retVal, EmitReturn(null));
            else
                return EmitCallBlock(OpCodes.Return, PrimitiveTypes.Void, CodeBlock.ToExpression(retVal));
        }

        private IReadOnlyList<WasmExpr> FlattenWasmBlock(WasmExpr Block)
        {
            if (Block is CallExpr)
            {
                var blockCall = (CallExpr)Block;
                if (blockCall.Target == OpCodes.Block)
                    return blockCall.Arguments;
            }
            return new WasmExpr[] { Block };
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            if (First is NopBlock)
            {
                return Second;
            }
            else if (Second is NopBlock)
            {
                return First;
            }
            else
            {
                var lBlock = (CodeBlock)First;
                var rBlock = (CodeBlock)Second;
                return EmitCallBlock(
                    OpCodes.Block,
                    lBlock.Type.Equals(PrimitiveTypes.Void) ? rBlock.Type : lBlock.Type,
                    FlattenWasmBlock(lBlock.Expression).Concat(FlattenWasmBlock(rBlock.Expression)).ToArray());
            }
        }

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            var val = (CodeBlock)Contents;

            return EmitCallBlock(
                OpCodes.Loop, val.Type,
                new IdentifierExpr(breakTags[Tag]),
                new IdentifierExpr(continueTags[Tag]),
                val.Expression);
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            var condBlock = (CodeBlock)Condition;
            var lBlock = (CodeBlock)IfBody;
            var rBlock = (CodeBlock)ElseBody;

            if (rBlock is NopBlock)
            {
                // emit:
                //     (if <cond> <if-body>)
                return EmitCallBlock(
                    OpCodes.If, PrimitiveTypes.Void,
                    condBlock.Expression, lBlock.Expression);
            }
            else if (lBlock is NopBlock)
            {
                // emit:
                //     (if (not <cond>) <else-body>)
                return EmitCallBlock(
                    OpCodes.If, PrimitiveTypes.Void,
                    CodeBlock.ToExpression(this.EmitNot(condBlock)), rBlock.Expression);
            }
            else
            {
                // emit:
                //     (if_else <cond> <if-body> <else-body>)
                return EmitCallBlock(
                    OpCodes.IfElse, lBlock.Type,
                    condBlock.Expression, lBlock.Expression, rBlock.Expression);
            }
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            var innerVal = (CodeBlock)Value;

            if (innerVal.Type.Equals(PrimitiveTypes.Void))
                // Nothing to pop here.
                return innerVal;
            else
                // emit:
                //     (block <expr> (nop))
                return EmitCallBlock(
                    OpCodes.Block, PrimitiveTypes.Void,
                    innerVal.Expression, new CallExpr(OpCodes.Nop));
        }

        #endregion

        #region Intrinsics

        private static readonly Dictionary<Tuple<IType, Operator>, Tuple<IType, OpCode>> unaryOps = new Dictionary<Tuple<IType, Operator>, Tuple<IType, OpCode>>()
        {
            // Unary negation for floating-point types.
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float32, OpCodes.Float32Negate) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float64, OpCodes.Float64Negate) },
        };

        private static readonly Dictionary<Tuple<IType, Operator>, Tuple<IType, OpCode>> binOps = new Dictionary<Tuple<IType, Operator>, Tuple<IType, OpCode>>()
        {
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Add), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32Add) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Add), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32Add) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Subtract), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32Subtract) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Subtract), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32Subtract) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Multiply), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32Multiply) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Multiply), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32Multiply) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Divide), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32DivideSigned) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Divide), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32DivideUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Remainder), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32RemainderSigned) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Remainder), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32RemainderUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.And), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32And) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.And), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32And) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Or), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32Or) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Or), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32Or) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Xor), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32Xor) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Xor), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32Xor) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.LeftShift), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32ShiftLeft) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.LeftShift), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32ShiftLeft) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.RightShift), Tuple.Create(PrimitiveTypes.Int32, OpCodes.Int32ShiftRightSigned) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.RightShift), Tuple.Create(PrimitiveTypes.UInt32, OpCodes.Int32ShiftRightUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32Equal) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32Equal) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32NotEqual) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32NotEqual) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32LessThanSigned) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32LessThanUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32LessThanOrEqualSigned) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32LessThanOrEqualUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32GreaterThanSigned) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32GreaterThanUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32GreaterThanOrEqualSigned) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int32GreaterThanOrEqualUnsigned) },

            { Tuple.Create(PrimitiveTypes.Int64, Operator.Add), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64Add) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Add), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64Add) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Subtract), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64Subtract) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Subtract), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64Subtract) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Multiply), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64Multiply) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Multiply), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64Multiply) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Divide), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64DivideSigned) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Divide), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64DivideUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Remainder), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64RemainderSigned) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Remainder), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64RemainderUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.And), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64And) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.And), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64And) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Or), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64Or) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Or), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64Or) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Xor), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64Xor) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Xor), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64Xor) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.LeftShift), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64ShiftLeft) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.LeftShift), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64ShiftLeft) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.RightShift), Tuple.Create(PrimitiveTypes.Int64, OpCodes.Int64ShiftRightSigned) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.RightShift), Tuple.Create(PrimitiveTypes.UInt64, OpCodes.Int64ShiftRightUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64Equal) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64Equal) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64NotEqual) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64NotEqual) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64LessThanSigned) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64LessThanUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64LessThanOrEqualSigned) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64LessThanOrEqualUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64GreaterThanSigned) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64GreaterThanUnsigned) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64GreaterThanOrEqualSigned) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Int64GreaterThanOrEqualUnsigned) },

            { Tuple.Create(PrimitiveTypes.Float32, Operator.Add), Tuple.Create(PrimitiveTypes.Float32, OpCodes.Float32Add) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float32, OpCodes.Float32Subtract) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Multiply), Tuple.Create(PrimitiveTypes.Float32, OpCodes.Float32Multiply) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Divide), Tuple.Create(PrimitiveTypes.Float32, OpCodes.Float32Divide) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float32Equal) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float32NotEqual) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float32LessThan) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float32LessThanOrEqual) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float32GreaterThan) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float32GreaterThanOrEqual) },

            { Tuple.Create(PrimitiveTypes.Float64, Operator.Add), Tuple.Create(PrimitiveTypes.Float64, OpCodes.Float64Add) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float64, OpCodes.Float64Subtract) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Multiply), Tuple.Create(PrimitiveTypes.Float64, OpCodes.Float64Multiply) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Divide), Tuple.Create(PrimitiveTypes.Float64, OpCodes.Float64Divide) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float64Equal) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float64NotEqual) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float64LessThan) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float64LessThanOrEqual) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float64GreaterThan) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, OpCodes.Float64GreaterThanOrEqual) }
        };

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            var val = (CodeBlock)Value;

            Tuple<IType, OpCode> wasmOp;
            if (Op.Equals(Operator.Not))
            {
                return EmitBinary(
                    val, new DefaultValueExpression(val.Type).Optimize().Emit(this),
                    Operator.CheckEquality);
            }
            else if (unaryOps.TryGetValue(Tuple.Create(val.Type, Op), out wasmOp))
            {
                return EmitCallBlock(
                    wasmOp.Item2, wasmOp.Item1,
                    val.Expression);
            }
            else
            {
                // Sorry. Can't do that.
                return null;
            }
        }

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            var lVal = (CodeBlock)A;
            var rVal = (CodeBlock)B;

            var lTy = lVal.Type;
            bool isPtr = lTy.GetIsPointer();
            if (isPtr)
                lTy = Abi.PointerIntegerType;
            if (lTy.Equals(PrimitiveTypes.Boolean))
                lTy = PrimitiveTypes.Int32;

            Tuple<IType, OpCode> wasmOp;
            if (binOps.TryGetValue(Tuple.Create(lTy, Op), out wasmOp))
            {
                return EmitCallBlock(
                    wasmOp.Item2,
                    isPtr && !wasmOp.Item1.Equals(PrimitiveTypes.Boolean)
                        ? lVal.Type
                        : wasmOp.Item1,
                    lVal.Expression, rVal.Expression);
            }
            else
            {
                // Sorry. Can't do that.
                return null;
            }
        }

        #endregion

        #region Casts

        private static readonly Dictionary<Tuple<IType, IType>, OpCode> staticCastOps = new Dictionary<Tuple<IType, IType>, OpCode>()
        {
            // Integer conversions
            { Tuple.Create(PrimitiveTypes.Int32, PrimitiveTypes.Int64), OpCodes.Int64ExtendInt32 },
            { Tuple.Create(PrimitiveTypes.UInt32, PrimitiveTypes.UInt64), OpCodes.Int64ExtendUInt32 },
            { Tuple.Create(PrimitiveTypes.Int64, PrimitiveTypes.Int32), OpCodes.Int32WrapInt64 },
            { Tuple.Create(PrimitiveTypes.UInt64, PrimitiveTypes.UInt32), OpCodes.Int32WrapInt64 },

            // Floating-point conversions
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Float64), OpCodes.Float64PromoteFloat32 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Float32), OpCodes.Float32DemoteFloat64 },

            // Mixed integer and floating-point conversions

            // - Float32 -> *
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Int32), OpCodes.Int32TruncateFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.UInt32), OpCodes.UInt32TruncateFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Int64), OpCodes.Int64TruncateFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.UInt64), OpCodes.UInt64TruncateFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Bit32), OpCodes.Int32ReinterpretFloat32 },

            // - Float64 -> *
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Int32), OpCodes.Int32TruncateFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.UInt32), OpCodes.UInt32TruncateFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Int64), OpCodes.Int64TruncateFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.UInt64), OpCodes.UInt64TruncateFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Bit64), OpCodes.Int64ReinterpretFloat64 },

            // - Int32 -> *
            { Tuple.Create(PrimitiveTypes.Int32, PrimitiveTypes.Float32), OpCodes.Float32ConvertInt32 },
            { Tuple.Create(PrimitiveTypes.Int32, PrimitiveTypes.Float64), OpCodes.Float64ConvertInt32 },

            // - UInt32 -> *
            { Tuple.Create(PrimitiveTypes.UInt32, PrimitiveTypes.Float32), OpCodes.Float32ConvertUInt32 },
            { Tuple.Create(PrimitiveTypes.UInt32, PrimitiveTypes.Float64), OpCodes.Float64ConvertUInt32 },

            // - Int64 -> *
            { Tuple.Create(PrimitiveTypes.Int64, PrimitiveTypes.Float32), OpCodes.Float32ConvertInt64 },
            { Tuple.Create(PrimitiveTypes.Int64, PrimitiveTypes.Float64), OpCodes.Float64ConvertInt64 },

            // - UInt64 -> *
            { Tuple.Create(PrimitiveTypes.UInt64, PrimitiveTypes.Float32), OpCodes.Float32ConvertUInt64 },
            { Tuple.Create(PrimitiveTypes.UInt64, PrimitiveTypes.Float64), OpCodes.Float64ConvertUInt64 },

            // - BitN -> *
            { Tuple.Create(PrimitiveTypes.Bit32, PrimitiveTypes.Float32), OpCodes.Float32ReinterpretInt32 },
            { Tuple.Create(PrimitiveTypes.Bit64, PrimitiveTypes.Float64), OpCodes.Float64ReinterpretInt64 },
        };

        private static readonly Dictionary<int, IType> intTys = new Dictionary<int, IType>()
        {
            { 8, PrimitiveTypes.Int8 },
            { 16, PrimitiveTypes.Int16 },
            { 32, PrimitiveTypes.Int32 },
            { 64, PrimitiveTypes.Int64 }
        };

        private static readonly Dictionary<int, IType> uintTys = new Dictionary<int, IType>()
        {
            { 8, PrimitiveTypes.UInt8 },
            { 16, PrimitiveTypes.UInt16 },
            { 32, PrimitiveTypes.UInt32 },
            { 64, PrimitiveTypes.UInt64 }
        };

        private static readonly Dictionary<int, IType> bitTys = new Dictionary<int, IType>()
        {
            { 8, PrimitiveTypes.Bit8 },
            { 16, PrimitiveTypes.Bit16 },
            { 32, PrimitiveTypes.Bit32 },
            { 64, PrimitiveTypes.Bit64 }
        };

        private CodeBlock EmitStaticCast(CodeBlock Value, IType FromType, IType ToType)
        {
            OpCode op;
            if (staticCastOps.TryGetValue(Tuple.Create(FromType, ToType), out op))
            {
                return EmitCallBlock(op, ToType, Value.Expression);
            }
            else
            {
                int fromSize = FromType.GetPrimitiveBitSize();

                bool fromBit = FromType.GetIsBit();
                bool toBit = ToType.GetIsBit();
                if (fromBit && toBit)
                {
                    return EmitStaticCast(Value, uintTys[fromSize], uintTys[ToType.GetPrimitiveBitSize()]);
                }
                else if (fromBit || toBit)
                {
                    return new ExprBlock(this, Value.Expression, ToType);
                }
                if (ToType.GetIsSignedInteger() && !FromType.GetIsSignedInteger())
                {
                    return EmitStaticCast(Value, intTys[fromSize], ToType);
                }
                else if (ToType.GetIsUnsignedInteger() && !FromType.GetIsUnsignedInteger())
                {
                    return EmitStaticCast(Value, uintTys[fromSize], ToType);
                }
                else if (fromSize < 32)
                {
                    if (FromType.GetIsSignedInteger())
                        return EmitStaticCast(Value, PrimitiveTypes.Int32, ToType);
                    else if (FromType.GetIsUnsignedInteger())
                        return EmitStaticCast(Value, PrimitiveTypes.UInt32, ToType);
                }
                else if (ToType.GetPrimitiveBitSize() < 32)
                {
                    if (ToType.GetIsSignedInteger())
                        return EmitStaticCast(Value, FromType, PrimitiveTypes.Int32);
                    else if (ToType.GetIsUnsignedInteger())
                        return EmitStaticCast(Value, FromType, PrimitiveTypes.UInt32);
                }
                return null;
            }
        }

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            var val = (CodeBlock)Value;
            if (Op.Equals(Operator.ReinterpretCast) || Op.Equals(Operator.DynamicCast))
            {
                // TODO: actually check the type for dynamic casts
                return new ExprBlock(this, val.Expression, Type);
            }
            else if (Op.Equals(Operator.StaticCast))
            {
                return EmitStaticCast(val, val.Type, Type);
            }
            else
            {
                // Sorry. Can't do that.
                return null;
            }
        }

        #endregion

        #region Methods

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            if (Method is DelegateBlock)
            {
                var delegBlock = (DelegateBlock)Method;
                var func = (WasmMethod)delegBlock.Method;
                if (delegBlock.Target == null)
                {
                    return EmitCallBlock(
                        OpCodes.Call, func.ReturnType,
                        new WasmExpr[] { new IdentifierExpr(func.WasmName) }
                            .Concat(Arguments.Select(CodeBlock.ToExpression))
                            .ToArray());
                }
            }
            throw new NotImplementedException();
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller, Operator Op)
        {
            return new DelegateBlock(this, (CodeBlock)Caller, Method, Op);
        }

        public ICodeBlock EmitNewObject(IMethod Constructor, IEnumerable<ICodeBlock> Arguments)
        {
            // new-object expressions should be lowered by
            // the pass pipeline.
            throw new InvalidOperationException();
        }

        #endregion

        #region Aggregates

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            // This requires malloc.
            throw new NotImplementedException();
        }

        public ICodeBlock EmitNewVector(IType ElementType, IReadOnlyList<int> Dimensions)
        {
            // This can be stack-allocated.
            throw new NotImplementedException();
        }

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            // This requires newarray/newvector to be implemented first.
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            // This requires newarray/newvector to be implemented first.
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            if (Target == null)
            {
                var wasmField = (WasmField)Field;
                return new MemoryLocation((CodeBlock)EmitTypeBinary(
                    new StaticCastExpression(
                        new IntegerExpression(wasmField.StaticStorageLocation.Offset),
                        Abi.PointerIntegerType).Simplify().Emit(this),
                    Field.FieldType.MakePointerType(PointerKind.ReferencePointer),
                    Operator.ReinterpretCast));
            }

            var targetBlock = (CodeBlock)Target;
            var layout = Abi.GetLayout(Field.DeclaringType);
            var rawAddress = this.EmitAdd(targetBlock, EmitInt32(layout.Members[Field].Offset));
            return new MemoryLocation((CodeBlock)EmitTypeBinary(
                rawAddress,
                Field.FieldType.MakePointerType(PointerKind.ReferencePointer),
                Operator.ReinterpretCast));
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return GetUnmanagedField(Field, Target);
        }

        #endregion

        #region Pointer magic

        private static readonly Dictionary<IType, OpCode> loadOpCodes = new Dictionary<IType, OpCode>()
        {
            { PrimitiveTypes.Int8, OpCodes.LoadInt8 },
            { PrimitiveTypes.UInt8, OpCodes.LoadUInt8 },
            { PrimitiveTypes.Bit8, OpCodes.LoadUInt8 },
            { PrimitiveTypes.Boolean, OpCodes.LoadUInt8 },
            { PrimitiveTypes.Int16, OpCodes.LoadInt16 },
            { PrimitiveTypes.UInt16, OpCodes.LoadUInt16 },
            { PrimitiveTypes.Bit16, OpCodes.LoadUInt16 },
            { PrimitiveTypes.Char, OpCodes.LoadUInt16 },
            { PrimitiveTypes.Int32, OpCodes.LoadInt32 },
            { PrimitiveTypes.UInt32, OpCodes.LoadInt32 },
            { PrimitiveTypes.Bit32, OpCodes.LoadInt32 },
            { PrimitiveTypes.Int64, OpCodes.LoadInt64 },
            { PrimitiveTypes.UInt64, OpCodes.LoadInt64 },
            { PrimitiveTypes.Bit64, OpCodes.LoadInt64 },
            { PrimitiveTypes.Float32, OpCodes.LoadFloat32 },
            { PrimitiveTypes.Float64, OpCodes.LoadFloat64 }
        };

        private static readonly Dictionary<IType, OpCode> storeOpCodes = new Dictionary<IType, OpCode>()
        {
            { PrimitiveTypes.Int8, OpCodes.StoreInt8 },
            { PrimitiveTypes.UInt8, OpCodes.StoreInt8 },
            { PrimitiveTypes.Bit8, OpCodes.StoreInt8 },
            { PrimitiveTypes.Boolean, OpCodes.StoreInt8 },
            { PrimitiveTypes.Int16, OpCodes.StoreInt16 },
            { PrimitiveTypes.UInt16, OpCodes.StoreInt16 },
            { PrimitiveTypes.Bit16, OpCodes.StoreInt16 },
            { PrimitiveTypes.Char, OpCodes.StoreInt16 },
            { PrimitiveTypes.Int32, OpCodes.StoreInt32 },
            { PrimitiveTypes.UInt32, OpCodes.StoreInt32 },
            { PrimitiveTypes.Bit32, OpCodes.StoreInt32 },
            { PrimitiveTypes.Int64, OpCodes.StoreInt64 },
            { PrimitiveTypes.UInt64, OpCodes.StoreInt64 },
            { PrimitiveTypes.Bit64, OpCodes.StoreInt64 },
            { PrimitiveTypes.Float32, OpCodes.StoreFloat32 },
            { PrimitiveTypes.Float64, OpCodes.StoreFloat64 }
        };

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            var targetBlock = (CodeBlock)Pointer;
            var elemTy = targetBlock.Type.AsPointerType().ElementType;

            OpCode loadOpCode;
            if (elemTy.GetIsPointer() || elemTy.Equals(PrimitiveTypes.Null))
                loadOpCode = loadOpCodes[Abi.PointerIntegerType];
            else
                loadOpCode = loadOpCodes[elemTy];

            return EmitCallBlock(loadOpCode, elemTy, targetBlock.Expression);
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return EmitInt32(Abi.GetLayout(Type).Size);
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            var targetBlock = (CodeBlock)Pointer;
            var elemTy = targetBlock.Type.AsPointerType().ElementType;

            OpCode storeOpCode;
            if (elemTy.GetIsPointer() || elemTy.Equals(PrimitiveTypes.Null))
                storeOpCode = storeOpCodes[Abi.PointerIntegerType];
            else
                storeOpCode = storeOpCodes[elemTy];

            return EmitCallBlock(storeOpCode, PrimitiveTypes.Void, targetBlock.Expression, CodeBlock.ToExpression(Value));
        }

        #endregion

        #region Variables

        private IEmitVariable CreateLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            if (VariableMember.VariableType.IsScalar())
            {
                var result = new Register(this, localNames[Tag], VariableMember.VariableType);
                registers.Add(result);
                return result;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public IEmitVariable DeclareLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            var local = CreateLocal(Tag, VariableMember);
            locals[Tag] = local;
            return local;
        }

        public IEmitVariable GetArgument(int Index)
        {
            var param = Method.Parameters.ElementAt(Index);
            var ty = param.ParameterType;
            if (ty.IsScalar())
            {
                return new Register(this, param.Name.ToString(), ty);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public IEmitVariable GetLocal(UniqueTag Tag)
        {
            IEmitVariable result;
            if (locals.TryGetValue(Tag, out result))
                return result;
            else
                return null;
        }

        public IEmitVariable GetThis()
        {
            return Abi.GetThisPointer(this);
        }

        public IUnmanagedEmitVariable DeclareUnmanagedLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedArgument(int Index)
        {
            throw new NotImplementedException();
        }

        public IUnmanagedEmitVariable GetUnmanagedLocal(UniqueTag Tag)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
