using System;
using System.Collections.Generic;
using System.Linq;
using Flame;
using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Variables;
using Flame.Compiler.Native;
using Flame.Compiler.Expressions;
using Wasm;
using Wasm.Instructions;
using WasmOperator = Wasm.Instructions.Operator;
using WasmAnyType = Wasm.WasmType;

namespace Flame.Wasm.Emit
{
    /// <summary>
    /// A code generator for wasm.
    /// </summary>
    public sealed class WasmCodeGenerator : ICodeGenerator, IUnmanagedCodeGenerator
    {
        public WasmCodeGenerator(IMethod Method, IWasmAbi Abi, FunctionType Signature)
        {
            this.Method = Method;
            this.Abi = Abi;
            this.locals = new Dictionary<UniqueTag, IEmitVariable>();
            this.localRegisters = new List<Register>();

            // Register reserved names
            this.abiRegisterCount = (uint)Signature.ParameterTypes.Count;
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
        private List<Register> localRegisters;
        private uint abiRegisterCount;

        #region Helpers

        public CodeBlock EmitInstructionBlock(CodeBlock Predecessor, Instruction Value, IType Type)
        {
            return new InstructionBlock(this, Predecessor, Value, Type);
        }

        public CodeBlock EmitInstructionBlock(Instruction Value, IType Type)
        {
            return new InstructionBlock(this, Value, Type);
        }

        #endregion

        #region Prologue/Epilogue

        /// <summary>
        /// Wraps the given body expression in a prologue for this
        /// function.
        /// </summary>
        public FunctionBody WrapBody(WasmExpr Body)
        {
            var locals = new List<LocalEntry>();
            foreach (var register in localRegisters)
            {
                locals.Add(new LocalEntry(WasmHelpers.GetWasmValueType(register.Type, Abi), 1));
            }
            return new FunctionBody(
                locals,
                Body.Append(Operators.Unreachable.Create()).ToInstructionList());
        }

        #endregion

        #region Constants

        private CodeBlock EmitTypedInt32(int Value, IType Type)
        {
            return EmitInstructionBlock(Operators.Int32Const.Create(Value), Type);
        }

        private CodeBlock EmitTypedInt64(long Value, IType Type)
        {
            return EmitInstructionBlock(Operators.Int64Const.Create(Value), Type);
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
            return EmitInstructionBlock(Operators.Float32Const.Create(Value), PrimitiveTypes.Float32);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return EmitInstructionBlock(Operators.Float64Const.Create(Value), PrimitiveTypes.Float64);
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
            return new FuncBlock(
                this,
                PrimitiveTypes.Void,
                (context, file) =>
                    new WasmExpr(
                        Operators.Br.Create(
                            context.GetDistance(
                                Target,
                                BlockContextKind.Block))));
        }

        public ICodeBlock EmitContinue(UniqueTag Target)
        {
            return new FuncBlock(
                this,
                PrimitiveTypes.Void,
                (context, file) =>
                    new WasmExpr(
                        Operators.Br.Create(
                            context.GetDistance(
                                Target,
                                BlockContextKind.Loop))));
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            var retVal = Value as CodeBlock;
            if (retVal != null && retVal.Type.Equals(PrimitiveTypes.Void))
                return EmitSequence(retVal, EmitReturn(null));
            else
                return EmitInstructionBlock(retVal, Operators.Return.Create(), PrimitiveTypes.Void);
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
                return new SequenceBlock(this, lBlock, rBlock);
            }
        }

        public ICodeBlock EmitTagged(UniqueTag Tag, ICodeBlock Contents)
        {
            var val = (CodeBlock)Contents;
            var valType = val.Type;
            var wasmType = WasmHelpers.GetWasmType(valType, Abi);

            // Wrap `val` in a `loop` and then in a `block`.
            return new FuncBlock(
                this,
                valType,
                (context, file) =>
                {
                    return new WasmExpr(
                        Operators.Block.Create(
                            wasmType,
                            new Instruction[]
                            {
                                Operators.Loop.Create(
                                    wasmType,
                                    val.ToExpression(
                                        context
                                        .CreateChild(Tag, BlockContextKind.Block)
                                        .CreateChild(Tag, BlockContextKind.Loop),
                                        file).ToInstructionList())
                            }));
                });
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
                return new FuncBlock(
                    this,
                    PrimitiveTypes.Void,
                    (context, file) =>
                        condBlock
                        .ToExpression(context, file)
                        .Append(
                            Operators.If.Create(
                                WasmAnyType.Empty,
                                lBlock.ToExpression(
                                    context.CreateChild(
                                        null,
                                        BlockContextKind.Block),
                                    file)
                                .ToInstructionList(),
                                null)));
            }
            else if (lBlock is NopBlock)
            {
                // emit:
                //     (if (not <cond>) <else-body>)
                return EmitIfElse(this.EmitNot(condBlock), ElseBody, IfBody);
            }
            else
            {
                // emit:
                //     (if_else <cond> <if-body> <else-body>)
                return new FuncBlock(
                    this,
                    lBlock.Type,
                    (context, file) =>
                        condBlock
                        .ToExpression(context, file)
                        .Append(
                            Operators.If.Create(
                                WasmHelpers.GetWasmType(lBlock.Type, Abi),
                                lBlock.ToExpression(
                                    context.CreateChild(
                                        null,
                                        BlockContextKind.Block),
                                    file)
                                .ToInstructionList(),
                                rBlock.ToExpression(
                                    context.CreateChild(
                                        null,
                                        BlockContextKind.Block),
                                    file)
                                .ToInstructionList())));
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
                //     <expr>
                //     drop
                return EmitInstructionBlock(
                    innerVal,
                    Operators.Drop.Create(),
                    PrimitiveTypes.Void);
        }

        #endregion

        #region Intrinsics

        private static readonly Dictionary<Tuple<IType, Operator>, Tuple<IType, NullaryOperator>> unaryOps =
            new Dictionary<Tuple<IType, Operator>, Tuple<IType, NullaryOperator>>()
        {
            // Unary negation for floating-point types.
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float32, Operators.Float32Neg) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float64, Operators.Float64Neg) },
        };

        private static readonly Dictionary<Tuple<IType, Operator>, Tuple<IType, NullaryOperator>> binOps =
            new Dictionary<Tuple<IType, Operator>, Tuple<IType, NullaryOperator>>()
        {
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Add), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32Add) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Add), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32Add) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Subtract), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32Sub) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Subtract), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32Sub) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Multiply), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32Mul) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Multiply), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32Mul) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Divide), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32DivS) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Divide), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32DivU) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Remainder), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32RemS) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Remainder), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32RemU) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.And), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32And) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.And), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32And) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Or), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32Or) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Or), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32Or) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.Xor), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32Xor) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.Xor), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32Xor) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.LeftShift), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32Shl) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.LeftShift), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32Shl) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.RightShift), Tuple.Create(PrimitiveTypes.Int32, Operators.Int32ShrS) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.RightShift), Tuple.Create(PrimitiveTypes.UInt32, Operators.Int32ShrU) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32Eq) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32Eq) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32Ne) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32Ne) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32LtS) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32LtU) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32LeS) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32LeU) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32GtS) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32GtU) },
            { Tuple.Create(PrimitiveTypes.Int32, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32GeS) },
            { Tuple.Create(PrimitiveTypes.UInt32, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int32GeU) },

            { Tuple.Create(PrimitiveTypes.Int64, Operator.Add), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64Add) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Add), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64Add) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Subtract), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64Sub) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Subtract), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64Sub) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Multiply), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64Mul) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Multiply), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64Mul) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Divide), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64DivS) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Divide), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64DivU) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Remainder), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64RemS) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Remainder), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64RemU) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.And), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64And) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.And), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64And) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Or), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64Or) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Or), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64Or) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.Xor), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64Xor) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.Xor), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64Xor) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.LeftShift), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64Shl) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.LeftShift), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64Shl) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.RightShift), Tuple.Create(PrimitiveTypes.Int64, Operators.Int64ShrS) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.RightShift), Tuple.Create(PrimitiveTypes.UInt64, Operators.Int64ShrU) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64Eq) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64Eq) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64Ne) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64Ne) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64LtS) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64LtU) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64LeS) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64LeU) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64GtS) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64GtU) },
            { Tuple.Create(PrimitiveTypes.Int64, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64GeS) },
            { Tuple.Create(PrimitiveTypes.UInt64, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Int64GeU) },

            { Tuple.Create(PrimitiveTypes.Float32, Operator.Add), Tuple.Create(PrimitiveTypes.Float32, Operators.Float32Add) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float32, Operators.Float32Sub) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Multiply), Tuple.Create(PrimitiveTypes.Float32, Operators.Float32Mul) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.Divide), Tuple.Create(PrimitiveTypes.Float32, Operators.Float32Div) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float32Eq) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float32Ne) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float32Lt) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float32Le) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float32Gt) },
            { Tuple.Create(PrimitiveTypes.Float32, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float32Ge) },

            { Tuple.Create(PrimitiveTypes.Float64, Operator.Add), Tuple.Create(PrimitiveTypes.Float64, Operators.Float64Add) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Subtract), Tuple.Create(PrimitiveTypes.Float64, Operators.Float64Sub) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Multiply), Tuple.Create(PrimitiveTypes.Float64, Operators.Float64Mul) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.Divide), Tuple.Create(PrimitiveTypes.Float64, Operators.Float64Div) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckEquality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float64Eq) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckInequality), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float64Ne) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckLessThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float64Lt) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckLessThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float64Le) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckGreaterThan), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float64Gt) },
            { Tuple.Create(PrimitiveTypes.Float64, Operator.CheckGreaterThanOrEqual), Tuple.Create(PrimitiveTypes.Boolean, Operators.Float64Ge) }
        };

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            var val = (CodeBlock)Value;

            Tuple<IType, NullaryOperator> wasmOp;
            if (Op.Equals(Operator.Not))
            {
                return EmitBinary(
                    val, new DefaultValueExpression(val.Type).Optimize().Emit(this),
                    Operator.CheckEquality);
            }
            else if (unaryOps.TryGetValue(Tuple.Create(val.Type, Op), out wasmOp))
            {
                return EmitInstructionBlock(val, wasmOp.Item2.Create(), wasmOp.Item1);
            }
            else
            {
                // Sorry. Can't do that.
                throw new Exception(string.Format("Operation {0} {1} not supported", Op, val.Type));
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

            Tuple<IType, NullaryOperator> wasmOp;
            if (binOps.TryGetValue(Tuple.Create(lTy, Op), out wasmOp))
            {
                return EmitInstructionBlock(
                    new SequenceBlock(this, lVal, rVal),
                    wasmOp.Item2.Create(),
                    isPtr && !wasmOp.Item1.Equals(PrimitiveTypes.Boolean)
                        ? lVal.Type
                        : wasmOp.Item1);
            }
            else
            {
                // Sorry. Can't do that.
                throw new Exception(string.Format("Operation {0} {1} {2} not supported", lVal.Type, Op, rVal.Type));
            }
        }

        #endregion

        #region Casts

        private static readonly Dictionary<Tuple<IType, IType>, NullaryOperator> staticCastOps =
            new Dictionary<Tuple<IType, IType>, NullaryOperator>()
        {
            // Integer conversions
            { Tuple.Create(PrimitiveTypes.Int32, PrimitiveTypes.Int64), Operators.Int64ExtendSInt32 },
            { Tuple.Create(PrimitiveTypes.UInt32, PrimitiveTypes.UInt64), Operators.Int64ExtendUInt32 },
            { Tuple.Create(PrimitiveTypes.Int64, PrimitiveTypes.Int32), Operators.Int32WrapInt64 },
            { Tuple.Create(PrimitiveTypes.UInt64, PrimitiveTypes.UInt32), Operators.Int32WrapInt64 },

            // Floating-point conversions
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Float64), Operators.Float64PromoteFloat32 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Float32), Operators.Float32DemoteFloat64 },

            // Mixed integer and floating-point conversions

            // - Float32 -> *
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Int32), Operators.Int32TruncSFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.UInt32), Operators.Int32TruncUFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Int64), Operators.Int64TruncSFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.UInt64), Operators.Int64TruncUFloat32 },
            { Tuple.Create(PrimitiveTypes.Float32, PrimitiveTypes.Bit32), Operators.Int32ReinterpretFloat32 },

            // - Float64 -> *
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Int32), Operators.Int32TruncSFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.UInt32), Operators.Int32TruncUFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Int64), Operators.Int64TruncSFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.UInt64), Operators.Int64TruncUFloat64 },
            { Tuple.Create(PrimitiveTypes.Float64, PrimitiveTypes.Bit64), Operators.Int64ReinterpretFloat64 },

            // - Int32 -> *
            { Tuple.Create(PrimitiveTypes.Int32, PrimitiveTypes.Float32), Operators.Float32ConvertSInt32 },
            { Tuple.Create(PrimitiveTypes.Int32, PrimitiveTypes.Float64), Operators.Float64ConvertSInt32 },

            // - UInt32 -> *
            { Tuple.Create(PrimitiveTypes.UInt32, PrimitiveTypes.Float32), Operators.Float32ConvertUInt32 },
            { Tuple.Create(PrimitiveTypes.UInt32, PrimitiveTypes.Float64), Operators.Float64ConvertUInt32 },

            // - Int64 -> *
            { Tuple.Create(PrimitiveTypes.Int64, PrimitiveTypes.Float32), Operators.Float32ConvertSInt64 },
            { Tuple.Create(PrimitiveTypes.Int64, PrimitiveTypes.Float64), Operators.Float64ConvertSInt64 },

            // - UInt64 -> *
            { Tuple.Create(PrimitiveTypes.UInt64, PrimitiveTypes.Float32), Operators.Float32ConvertUInt64 },
            { Tuple.Create(PrimitiveTypes.UInt64, PrimitiveTypes.Float64), Operators.Float64ConvertUInt64 },

            // - BitN -> *
            { Tuple.Create(PrimitiveTypes.Bit32, PrimitiveTypes.Float32), Operators.Float32ReinterpretInt32 },
            { Tuple.Create(PrimitiveTypes.Bit64, PrimitiveTypes.Float64), Operators.Float64ReinterpretInt64 },
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
            NullaryOperator op;
            if (staticCastOps.TryGetValue(Tuple.Create(FromType, ToType), out op))
            {
                return EmitInstructionBlock(Value, op.Create(), ToType);
            }
            else if (FromType.Equals(ToType))
            {
                return Value;
            }
            else if (PrimitiveTypes.Char.Equals(FromType))
            {
                return EmitStaticCast(
                    new RetypedBlock(Value, PrimitiveTypes.UInt32),
                    PrimitiveTypes.UInt32,
                    ToType);
            }
            else if (PrimitiveTypes.Char.Equals(ToType))
            {
                return new RetypedBlock(
                    EmitStaticCast(
                        Value,
                        FromType,
                        PrimitiveTypes.Int32),
                    ToType);
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
                    return new RetypedBlock(Value, ToType);
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
                throw new Exception(string.Format("Cannot convert {0} to {1}", FromType, ToType));
            }
        }

        public ICodeBlock EmitTypeBinary(ICodeBlock Value, IType Type, Operator Op)
        {
            var val = (CodeBlock)Value;
            if (Op.Equals(Operator.ReinterpretCast) || Op.Equals(Operator.DynamicCast))
            {
                // TODO: actually check the type for dynamic casts
                return new RetypedBlock(val, Type);
            }
            else if (Op.Equals(Operator.StaticCast))
            {
                return EmitStaticCast(val, val.Type, Type);
            }
            else
            {
                // Sorry. Can't do that.
                throw new Exception(string.Format("Operation {0} {1} {2} not supported", val.Type, Op, Type));
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
                    return new CallBlock(this, func, func.ReturnType, Arguments.Cast<CodeBlock>().ToArray());
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

        private static readonly Dictionary<IType, MemoryOperator> loadOperators = new Dictionary<IType, MemoryOperator>()
        {
            { PrimitiveTypes.Int8, Operators.Int32Load8S },
            { PrimitiveTypes.UInt8, Operators.Int32Load8U },
            { PrimitiveTypes.Bit8, Operators.Int32Load8U },
            { PrimitiveTypes.Boolean, Operators.Int32Load8U },
            { PrimitiveTypes.Int16, Operators.Int32Load16S },
            { PrimitiveTypes.UInt16, Operators.Int32Load16U },
            { PrimitiveTypes.Bit16, Operators.Int32Load16U },
            { PrimitiveTypes.Char, Operators.Int32Load16U },
            { PrimitiveTypes.Int32, Operators.Int32Load },
            { PrimitiveTypes.UInt32, Operators.Int32Load },
            { PrimitiveTypes.Bit32, Operators.Int32Load },
            { PrimitiveTypes.Int64, Operators.Int64Load },
            { PrimitiveTypes.UInt64, Operators.Int64Load },
            { PrimitiveTypes.Bit64, Operators.Int64Load },
            { PrimitiveTypes.Float32, Operators.Float32Load },
            { PrimitiveTypes.Float64, Operators.Float64Load }
        };

        private static readonly Dictionary<IType, MemoryOperator> storeOperators = new Dictionary<IType, MemoryOperator>()
        {
            { PrimitiveTypes.Int8, Operators.Int32Store8 },
            { PrimitiveTypes.UInt8, Operators.Int32Store8 },
            { PrimitiveTypes.Bit8, Operators.Int32Store8 },
            { PrimitiveTypes.Boolean, Operators.Int32Store8 },
            { PrimitiveTypes.Int16, Operators.Int32Store16 },
            { PrimitiveTypes.UInt16, Operators.Int32Store16 },
            { PrimitiveTypes.Bit16, Operators.Int32Store16 },
            { PrimitiveTypes.Char, Operators.Int32Store16 },
            { PrimitiveTypes.Int32, Operators.Int32Store },
            { PrimitiveTypes.UInt32, Operators.Int32Store },
            { PrimitiveTypes.Bit32, Operators.Int32Store },
            { PrimitiveTypes.Int64, Operators.Int64Store },
            { PrimitiveTypes.UInt64, Operators.Int64Store },
            { PrimitiveTypes.Bit64, Operators.Int64Store },
            { PrimitiveTypes.Float32, Operators.Float32Store },
            { PrimitiveTypes.Float64, Operators.Float64Store }
        };

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            var targetBlock = (CodeBlock)Pointer;
            var elemTy = targetBlock.Type.AsPointerType().ElementType;

            MemoryOperator loadWasmOperator;
            if (elemTy.GetIsPointer() || elemTy.Equals(PrimitiveTypes.Null))
                loadWasmOperator = loadOperators[Abi.PointerIntegerType];
            else
                loadWasmOperator = loadOperators[elemTy];

            return EmitInstructionBlock(targetBlock, loadWasmOperator.Create(0, 0), elemTy);
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return EmitInt32(Abi.GetLayout(Type).Size);
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            var targetBlock = (CodeBlock)Pointer;
            var elemTy = targetBlock.Type.AsPointerType().ElementType;

            MemoryOperator storeWasmOperator;
            if (elemTy.GetIsPointer() || elemTy.Equals(PrimitiveTypes.Null))
                storeWasmOperator = storeOperators[Abi.PointerIntegerType];
            else
                storeWasmOperator = storeOperators[elemTy];

            return EmitInstructionBlock(
                new SequenceBlock(this, targetBlock, (CodeBlock)Value),
                storeWasmOperator.Create(0, 0),
                PrimitiveTypes.Void);
        }

        #endregion

        #region Variables

        private IEmitVariable CreateLocal(UniqueTag Tag, IVariableMember VariableMember)
        {
            if (VariableMember.VariableType.IsScalar())
            {
                var result = new Register(
                    this,
                    (uint)localRegisters.Count + abiRegisterCount,
                    VariableMember.VariableType);
                localRegisters.Add(result);
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
            return Abi.GetArgument(this, Index);
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
