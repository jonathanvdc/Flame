using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm.Emit
{
    internal sealed class MethodBodyEmitter
    {
        public MethodBodyEmitter(ModuleBuilder module, LLVMValueRef function)
        {
            this.Module = module;
            this.Function = function;
            this.emittedBlocks = new Dictionary<BasicBlockTag, LLVMBasicBlockRef>();
            this.emittedValues = new Dictionary<ValueTag, LLVMValueRef>();
            this.blockBuilders = new Dictionary<BasicBlockTag, IRBuilder>();
            this.filledBlocks = new HashSet<BasicBlockTag>();
        }

        public ModuleBuilder Module { get; private set; }

        public LLVMValueRef Function { get; private set; }

        private Dictionary<ValueTag, LLVMValueRef> emittedValues;
        private Dictionary<BasicBlockTag, LLVMBasicBlockRef> emittedBlocks;
        private Dictionary<BasicBlockTag, IRBuilder> blockBuilders;
        private HashSet<BasicBlockTag> filledBlocks;

        public void Emit(MethodBody body)
        {
            var preds = body.Implementation.GetAnalysisResult<BasicBlockPredecessors>();
            var entry = body.Implementation.EntryPoint;

            if (preds.GetPredecessorsOf(entry).Any())
            {
                // Create a thunk block.
                var entryThunk = Function.AppendBasicBlock(entry.Tag.Name + ".thunk");

                // Emit the body's blocks.
                PlaceBlocks(body);

                // Jump to the entry point.
                FillJumpThunk(entryThunk, entry, Function.GetParams());
            }
            else
            {
                // Emit the entry point first.
                var llvmBlock = emittedBlocks[entry] = Function.AppendBasicBlock(entry.Tag.Name);
                var blockBuilder = blockBuilders[entry] = new IRBuilder(Module.Context);
                blockBuilder.PositionBuilderAtEnd(llvmBlock);

                // Set the entry point's parameters to the function's parameters.
                for (int i = 0; i < entry.Parameters.Count; i++)
                {
                    emittedValues[entry.Parameters[i]] = Function.GetParam((uint)i);
                }

                // Place the other blocks.
                PlaceBlocks(body);

                // Emit the blocks' contents.
                Emit(entry);
            }
        }

        private void PlaceBlocks(MethodBody body)
        {
            foreach (var block in body.Implementation.BasicBlocks)
            {
                if (emittedBlocks.ContainsKey(block))
                {
                    continue;
                }

                var llvmBlock = emittedBlocks[block] = Function.AppendBasicBlock(block.Tag.Name);
                var blockBuilder = blockBuilders[block] = new IRBuilder(Module.Context);
                blockBuilder.PositionBuilderAtEnd(llvmBlock);

                // Create one phi per block parameter.
                foreach (var param in block.Parameters)
                {
                    emittedValues[param] = blockBuilder.CreatePhi(
                        Module.ImportType(param.Type),
                        param.Tag.Name);
                }
            }
        }

        public LLVMBasicBlockRef Emit(BasicBlock block)
        {
            var llvmBlock = emittedBlocks[block];
            if (!filledBlocks.Add(block))
            {
                return llvmBlock;
            }

            var builder = blockBuilders[block];
            foreach (var instruction in block.NamedInstructions)
            {
                var val = emittedValues[instruction] = Emit(
                    instruction.Instruction,
                    builder,
                    instruction.ResultType == Module.TypeSystem.Void
                        ? ""
                        : instruction.Tag.Name);
            }
            Emit(block.Flow, block.Graph, builder);
            return llvmBlock;
        }

        private LLVMValueRef Get(ValueTag value)
        {
            return emittedValues[value];
        }

        private LLVMValueRef Emit(Instruction instruction, IRBuilder builder, string name)
        {
            var proto = instruction.Prototype;
            if (proto is ConstantPrototype)
            {
                return EmitConstant((ConstantPrototype)proto, builder);
            }
            else if (proto is CopyPrototype)
            {
                return Get(instruction.Arguments[0]);
            }
            else if (proto is ReinterpretCastPrototype)
            {
                return builder.CreateBitCast(
                    Get(instruction.Arguments[0]),
                    Module.ImportType(instruction.ResultType),
                    name);
            }
            else if (proto is DynamicCastPrototype)
            {
                // There are three possible outcomes here:
                //   1. We receive a pointer of the expected type. We return a reineterpreted pointer.
                //   2. We receive a pointer of another type. We return null.
                //   3. We receive a null pointer. We return null.
                //
                // Case #3 is kind of ugly, because it means that we need to null-test the input
                // pointer and only load its metadata if it is nonnull.

                // Create two extra basic blocks.
                var forkBlock = builder.GetInsertBlock();
                var nonnullBlock = Function.AppendBasicBlock(name + ".nonnull");
                var mergeBlock = Function.AppendBasicBlock(name + ".merge");

                // Emit a null check and branch based on that.
                var originalPtr = Get(instruction.Arguments[0]);
                builder.CreateCondBr(
                    builder.CreateIsNull(originalPtr, name + ".isnull"),
                    mergeBlock,
                    nonnullBlock);

                // Fill the nonnull block.
                builder.PositionBuilderAtEnd(nonnullBlock);
                var resultType = Module.ImportType(instruction.ResultType);
                var reinterpretedPtr = builder.CreateBitCast(
                    originalPtr,
                    resultType,
                    name + ".reinterpreted");
                var isinst = Module.Metadata.EmitIsSubtype(
                    Module.GC.EmitLoadMetadata(reinterpretedPtr, Module, builder, name + ".metadata"),
                    ((DynamicCastPrototype)proto).TargetType.ElementType,
                    Module,
                    builder,
                    name + ".isinst");
                var nullConst = LLVM.ConstNull(resultType);
                var nonnullResult = builder.CreateSelect(
                    isinst, reinterpretedPtr, nullConst, name + ".nonnull.result");
                builder.CreateBr(mergeBlock);

                // Fill the merge block.
                builder.PositionBuilderAtEnd(mergeBlock);
                var result = builder.CreatePhi(resultType, name);
                result.AddIncoming(
                    new[] { nonnullResult, nullConst },
                    new[] { nonnullBlock, forkBlock },
                    2);
                return result;
            }
            else if (proto is LoadPrototype)
            {
                return builder.CreateLoad(Get(instruction.Arguments[0]), name);
            }
            else if (proto is StorePrototype)
            {
                var storeProto = (StorePrototype)proto;
                return builder.CreateStore(
                    Get(storeProto.GetValue(instruction)),
                    Get(storeProto.GetPointer(instruction)));
            }
            else if (proto is AllocaPrototype)
            {
                var allocaProto = (AllocaPrototype)proto;
                return builder.CreateAlloca(Module.ImportType(allocaProto.ElementType), name);
            }
            else if (proto is AllocaArrayPrototype)
            {
                var allocaProto = (AllocaArrayPrototype)proto;
                return builder.CreateArrayAlloca(
                    Module.ImportType(allocaProto.ElementType),
                    Get(allocaProto.GetElementCount(instruction)),
                    name);
            }
            else if (proto is GetFieldPointerPrototype)
            {
                var gfp = (GetFieldPointerPrototype)proto;
                return builder.CreateStructGEP(
                    Get(gfp.GetBasePointer(instruction)),
                    (uint)Module.GetFieldIndex(gfp.Field),
                    name);
            }
            else if (proto is GetStaticFieldPointerPrototype)
            {
                var gfp = (GetStaticFieldPointerPrototype)proto;
                return Module.DefineStaticField(gfp.Field);
            }
            else if (proto is CallPrototype)
            {
                var callProto = (CallPrototype)proto;
                var args = instruction.Arguments.Select(Get).ToArray();
                var thisPtr = callProto.Lookup == MethodLookup.Virtual
                    ? Get(callProto.GetThisArgument(instruction))
                    : new LLVMValueRef(IntPtr.Zero);
                var functionPtr = LookupMethod(
                    callProto.Callee,
                    callProto.Lookup,
                    thisPtr,
                    builder);
                return builder.CreateCall(functionPtr, args, name);
            }
            else if (proto is NewDelegatePrototype)
            {
                var newDelegProto = (NewDelegatePrototype)proto;
                var thisPtr = newDelegProto.HasThisArgument
                    ? Get(newDelegProto.GetThisArgument(instruction))
                    : new LLVMValueRef(IntPtr.Zero);
                var functionPtr = LookupMethod(
                    newDelegProto.Callee,
                    newDelegProto.Lookup,
                    thisPtr,
                    builder);
                return Module.GC.EmitAllocDelegate(newDelegProto.ResultType, functionPtr, thisPtr, Module, builder, name);
            }
            else if (proto is NewObjectPrototype)
            {
                var newobjProto = (NewObjectPrototype)proto;
                var ctor = newobjProto.Constructor;
                var instance = Module.GC.EmitAllocObject(ctor.ParentType, Module, builder, name);
                builder.CreateCall(
                    Module.DeclareMethod(ctor),
                    new[] { instance }.Concat(instruction.Arguments.Select(Get)).ToArray(),
                    "");
                return instance;
            }
            else if (proto is IntrinsicPrototype)
            {
                return EmitIntrinsic((IntrinsicPrototype)proto, instruction.Arguments, builder, name);
            }
            else
            {
                throw new NotSupportedException($"Unsupported instruction prototype '{proto}'.");
            }
        }

        private LLVMValueRef LookupMethod(
            IMethod callee,
            MethodLookup lookup,
            LLVMValueRef thisPtr,
            IRBuilder builder)
        {
            if (lookup == MethodLookup.Static)
            {
                return Module.DeclareMethod(callee);
            }
            else
            {
                var metadataPtr = Module.GC.EmitLoadMetadata(thisPtr, Module, builder, "vtable.ptr");
                return Module.Metadata.EmitMethodAddress(callee, metadataPtr, Module, builder, "vfptr");
            }
        }

        private LLVMValueRef EmitIntrinsic(
            IntrinsicPrototype prototype,
            IReadOnlyList<ValueTag> arguments,
            IRBuilder builder,
            string name)
        {
            string opName;
            if (ArithmeticIntrinsics.TryParseArithmeticIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (prototype.ParameterCount == 1)
                {
                    if (opName == ArithmeticIntrinsics.Operators.Convert)
                    {
                        return EmitConvert(
                            Get(arguments[0]),
                            prototype.ParameterTypes[0],
                            prototype.ResultType,
                            builder,
                            name);
                    }
                }
                else if (prototype.ParameterCount == 2)
                {
                    if (opName == ArithmeticIntrinsics.Operators.Add)
                    {
                        if (prototype.ResultType.IsIntegerType())
                        {
                            return builder.CreateAdd(Get(arguments[0]), Get(arguments[1]), name);
                        }
                        else if (prototype.ResultType == Module.TypeSystem.Float32
                            || prototype.ResultType == Module.TypeSystem.Float64)
                        {
                            return builder.CreateFAdd(Get(arguments[0]), Get(arguments[1]), name);
                        }
                        else if (prototype.ResultType.IsPointerType()
                            && prototype.ParameterTypes[0].IsPointerType())
                        {
                            var i8Base = builder.CreateBitCast(
                                Get(arguments[0]),
                                LLVM.PointerType(LLVM.Int8TypeInContext(Module.Context), 0),
                                name + ".base");
                            var i8Result = builder.CreateGEP(i8Base, new[] { Get(arguments[1]) }, name + ".raw");
                            return builder.CreateBitCast(i8Result, Module.ImportType(prototype.ResultType), name);
                        }
                    }
                    else if (opName == ArithmeticIntrinsics.Operators.IsEqualTo)
                    {
                        return EmitAreEqual(Get(arguments[0]), Get(arguments[1]), builder, name);
                    }
                    else if (prototype.ParameterTypes[0].IsSignedIntegerType()
                        && prototype.ParameterTypes[1].IsSignedIntegerType())
                    {
                        LLVMIntPredicate predicate;
                        if (signedIntPredicates.TryGetValue(opName, out predicate))
                        {
                            return builder.CreateICmp(predicate, Get(arguments[0]), Get(arguments[1]), name);
                        }
                        LLVMOpcode opcode;
                        if (signedIntOps.TryGetValue(opName, out opcode))
                        {
                            return builder.CreateBinOp(opcode, Get(arguments[0]), Get(arguments[1]), name);
                        }
                    }
                    else if (prototype.ParameterTypes[0].IsUnsignedIntegerType()
                        && prototype.ParameterTypes[1].IsUnsignedIntegerType())
                    {
                        LLVMIntPredicate predicate;
                        if (unsignedIntPredicates.TryGetValue(opName, out predicate))
                        {
                            return builder.CreateICmp(predicate, Get(arguments[0]), Get(arguments[1]), name);
                        }
                        LLVMOpcode opcode;
                        if (unsignedIntOps.TryGetValue(opName, out opcode))
                        {
                            return builder.CreateBinOp(opcode, Get(arguments[0]), Get(arguments[1]), name);
                        }
                    }
                    else if (IsFloatingPointType(prototype.ParameterTypes[0])
                        && IsFloatingPointType(prototype.ParameterTypes[1]))
                    {
                        LLVMRealPredicate predicate;
                        if (floatPredicates.TryGetValue(opName, out predicate))
                        {
                            return builder.CreateFCmp(predicate, Get(arguments[0]), Get(arguments[1]), name);
                        }
                        LLVMOpcode opcode;
                        if (floatOps.TryGetValue(opName, out opcode))
                        {
                            return builder.CreateBinOp(opcode, Get(arguments[0]), Get(arguments[1]), name);
                        }
                    }
                }
            }
            else if (MemoryIntrinsics.Namespace.TryParseIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (prototype.ParameterCount == 0)
                {
                    if (opName == MemoryIntrinsics.Operators.AllocaPinned)
                    {
                        // If pinned allocas haven't been lowered by now, then we will
                        // simply treat them as regular allocas.
                        var elementType = ((PointerType)prototype.ResultType).ElementType;
                        return builder.CreateAlloca(Module.ImportType(elementType), name);
                    }
                }
            }
            else if (ArrayIntrinsics.Namespace.TryParseIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (opName == ArrayIntrinsics.Operators.NewArray)
                {
                    var arrayType = ((PointerType)prototype.ResultType).ElementType;
                    // TODO: handle non-generic array types?
                    var elementType = arrayType.GetGenericArguments()[0];
                    return Module.GC.EmitAllocArray(
                        arrayType,
                        elementType,
                        arguments.Select(Get).ToArray(),
                        Module,
                        builder,
                        name);
                }
                else if (opName == ArrayIntrinsics.Operators.GetElementPointer)
                {
                    var elementType = ((PointerType)prototype.ResultType).ElementType;
                    return Module.GC.EmitArrayElementAddress(
                        Get(arguments[0]),
                        elementType,
                        arguments.Skip(1).Select(Get).ToArray(),
                        Module,
                        builder,
                        name);
                }
                else if (opName == ArrayIntrinsics.Operators.LoadElement)
                {
                    var ptr = Module.GC.EmitArrayElementAddress(
                        Get(arguments[0]),
                        prototype.ResultType,
                        arguments.Skip(1).Select(Get).ToArray(),
                        Module,
                        builder,
                        name + ".ref");
                    return builder.CreateLoad(ptr, name);
                }
                else if (opName == ArrayIntrinsics.Operators.StoreElement)
                {
                    var ptr = Module.GC.EmitArrayElementAddress(
                        Get(arguments[1]),
                        prototype.ResultType,
                        arguments.Skip(2).Select(Get).ToArray(),
                        Module,
                        builder,
                        name + ".ref");
                    var val = Get(arguments[0]);
                    builder.CreateStore(val, ptr);
                    return val;
                }
                else if (opName == ArrayIntrinsics.Operators.GetLength
                    && arguments.Count == 1)
                {
                    var arrayType = ((PointerType)prototype.ParameterTypes[0]).ElementType;
                    // TODO: handle non-generic array types?
                    var elementType = arrayType.GetGenericArguments()[0];
                    // TODO: detect dimensionality of the array.
                    int dims = 1;
                    var result = Module.GC.EmitArrayLength(
                        Get(arguments[0]),
                        elementType,
                        dims,
                        Module,
                        builder,
                        name);
                    var resultType = Module.ImportType(prototype.ResultType);
                    if (resultType.TypeKind == LLVMTypeKind.LLVMPointerTypeKind)
                    {
                        return builder.CreateIntToPtr(result, resultType, "");
                    }
                    else
                    {
                        return builder.CreateIntCast(result, resultType, "");
                    }
                }
            }
            throw new NotSupportedException($"Unsupported intrinsic '{prototype.Name}'.");
        }

        private LLVMValueRef EmitConvert(
            LLVMValueRef value,
            IType from,
            IType to,
            IRBuilder builder,
            string name)
        {
            var llvmTo = Module.ImportType(to);
            if (IsFloatingPointType(from))
            {
                if (IsFloatingPointType(to))
                {
                    return builder.CreateFPCast(value, llvmTo, name);
                }
                else if (to.IsSignedIntegerType())
                {
                    return builder.CreateFPToSI(value, llvmTo, name);
                }
                else if (to.IsUnsignedIntegerType())
                {
                    return builder.CreateFPToUI(value, llvmTo, name);
                }
            }
            else if (from.IsSignedIntegerType())
            {
                if (IsFloatingPointType(to))
                {
                    return builder.CreateSIToFP(value, llvmTo, name);
                }
                else if (llvmTo.TypeKind == LLVMTypeKind.LLVMPointerTypeKind)
                {
                    return builder.CreateIntToPtr(value, llvmTo, name);
                }
            }
            else if (from.IsUnsignedIntegerType())
            {
                if (IsFloatingPointType(to))
                {
                    return builder.CreateUIToFP(value, llvmTo, name);
                }
                else if (llvmTo.TypeKind == LLVMTypeKind.LLVMPointerTypeKind)
                {
                    return builder.CreateIntToPtr(value, llvmTo, name);
                }
            }
            else if (value.TypeOf().TypeKind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                if (to.IsIntegerType())
                {
                    return builder.CreatePtrToInt(value, llvmTo, name);
                }
                else if (llvmTo.TypeKind == LLVMTypeKind.LLVMPointerTypeKind)
                {
                    return builder.CreateBitCast(value, llvmTo, name);
                }
            }
            throw new NotSupportedException(
                $"Unsupported conversion of a '{from.FullName}' ('{value.TypeOf()}') to a '{to.FullName}' ('{llvmTo}').");
        }

        private bool IsFloatingPointType(IType type)
        {
            return type == Module.TypeSystem.Float32
                || type == Module.TypeSystem.Float64;
        }

        private LLVMValueRef EmitConstant(ConstantPrototype prototype, IRBuilder builder)
        {
            if (prototype.Value is IntegerConstant)
            {
                var intConst = (IntegerConstant)prototype.Value;
                return LLVM.ConstInt(
                    Module.ImportType(prototype.ResultType),
                    intConst.ToUInt64(),
                    intConst.Spec.IsSigned);
            }
            else if (prototype.Value == NullConstant.Instance
                || prototype.Value == DefaultConstant.Instance)
            {
                return LLVM.ConstNull(Module.ImportType(prototype.ResultType));
            }
            else if (prototype.Value is Float32Constant)
            {
                return LLVM.ConstReal(
                    LLVM.FloatTypeInContext(Module.Context),
                    ((Float32Constant)prototype.Value).Value);
            }
            else if (prototype.Value is Float64Constant)
            {
                return LLVM.ConstReal(
                    LLVM.DoubleTypeInContext(Module.Context),
                    ((Float64Constant)prototype.Value).Value);
            }
            else
            {
                throw new NotSupportedException($"Unsupported constant '{prototype.Value}'.");
            }
        }

        private void Emit(BlockFlow flow, FlowGraph graph, IRBuilder builder)
        {
            if (flow is ReturnFlow)
            {
                var insn = ((ReturnFlow)flow).ReturnValue;
                var val = Emit(insn, builder, "retval");
                if (insn.ResultType == Module.TypeSystem.Void)
                {
                    builder.CreateRetVoid();
                }
                else
                {
                    builder.CreateRet(val);
                }
            }
            else if (flow is JumpFlow)
            {
                var branch = ((JumpFlow)flow).Branch;
                builder.CreateBr(CreateJumpThunk(branch, graph));
            }
            else if (flow is SwitchFlow)
            {
                var switchFlow = (SwitchFlow)flow;
                var switchVal = Emit(switchFlow.SwitchValue, builder, "switchval");
                if (switchFlow.IsIfElseFlow)
                {
                    var cmp = EmitAreEqual(
                        switchVal,
                        Emit(
                            Instruction.CreateConstant(switchFlow.Cases[0].Values.Single(), switchFlow.SwitchValue.ResultType),
                            builder,
                            "const"),
                        builder,
                        "condition");

                    builder.CreateCondBr(
                        cmp,
                        CreateJumpThunk(switchFlow.Cases[0].Branch, graph),
                        CreateJumpThunk(switchFlow.DefaultBranch, graph));
                }
                else
                {
                    throw new NotSupportedException($"Unsupported block flow '{flow}'.");
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported block flow '{flow}'.");
            }
        }

        private LLVMValueRef EmitAreEqual(
            LLVMValueRef lhs,
            LLVMValueRef rhs,
            IRBuilder builder,
            string name)
        {
            var lhsType = lhs.TypeOf();
            var rhsType = rhs.TypeOf();
            if (lhsType.TypeKind != rhsType.TypeKind)
            {
                throw new NotSupportedException($"Cannot compare '{lhsType}' and '{rhsType}' instances.");
            }
            switch (lhsType.TypeKind)
            {
                case LLVMTypeKind.LLVMIntegerTypeKind:
                case LLVMTypeKind.LLVMPointerTypeKind:
                    return builder.CreateICmp(LLVMIntPredicate.LLVMIntEQ, lhs, rhs, name);
                case LLVMTypeKind.LLVMFloatTypeKind:
                    return builder.CreateFCmp(LLVMRealPredicate.LLVMRealOEQ, lhs, rhs, name);
                default:
                    throw new NotSupportedException($"Cannot compare '{lhsType}' instances.");
            }
        }

        private LLVMBasicBlockRef CreateJumpThunk(
            BasicBlock target,
            IReadOnlyList<LLVMValueRef> arguments)
        {
            var block = Function.AppendBasicBlock(target.Tag.Name + ".thunk");
            FillJumpThunk(block, target, arguments);
            return block;
        }

        private LLVMBasicBlockRef CreateJumpThunk(Branch branch, FlowGraph graph)
        {
            var target = graph.GetBasicBlock(branch.Target);
            if (branch.Arguments.Count == 0)
            {
                return Emit(target);
            }
            else
            {
                return CreateJumpThunk(
                    target,
                    branch.Arguments.Select(arg => Get(arg.ValueOrNull)).ToArray());
            }
        }

        private void FillJumpThunk(
            LLVMBasicBlockRef thunk,
            BasicBlock target,
            IReadOnlyList<LLVMValueRef> arguments)
        {
            using (var builder = new IRBuilder(Module.Context))
            {
                builder.PositionBuilderAtEnd(thunk);
                builder.CreateBr(Emit(target));
            }
            for (int i = 0; i < target.Parameters.Count; i++)
            {
                var phi = emittedValues[target.Parameters[i]];
                phi.AddIncoming(new[] { arguments[i] }, new[] { thunk }, 1);
            }
        }

        // A mapping of signed arithmetic intrinsic ops to integer predicates.
        private static Dictionary<string, LLVMIntPredicate> signedIntPredicates =
            new Dictionary<string, LLVMIntPredicate>()
        {
            { ArithmeticIntrinsics.Operators.IsEqualTo, LLVMIntPredicate.LLVMIntEQ },
            { ArithmeticIntrinsics.Operators.IsNotEqualTo, LLVMIntPredicate.LLVMIntNE },
            { ArithmeticIntrinsics.Operators.IsGreaterThanOrEqualTo, LLVMIntPredicate.LLVMIntSGE },
            { ArithmeticIntrinsics.Operators.IsGreaterThan, LLVMIntPredicate.LLVMIntSGT },
            { ArithmeticIntrinsics.Operators.IsLessThanOrEqualTo, LLVMIntPredicate.LLVMIntSLE },
            { ArithmeticIntrinsics.Operators.IsLessThan, LLVMIntPredicate.LLVMIntSLT }
        };

        // A mapping of unsigned arithmetic intrinsic ops to integer predicates.
        private static Dictionary<string, LLVMIntPredicate> unsignedIntPredicates =
            new Dictionary<string, LLVMIntPredicate>()
        {
            { ArithmeticIntrinsics.Operators.IsEqualTo, LLVMIntPredicate.LLVMIntEQ },
            { ArithmeticIntrinsics.Operators.IsNotEqualTo, LLVMIntPredicate.LLVMIntNE },
            { ArithmeticIntrinsics.Operators.IsGreaterThanOrEqualTo, LLVMIntPredicate.LLVMIntUGE },
            { ArithmeticIntrinsics.Operators.IsGreaterThan, LLVMIntPredicate.LLVMIntUGT },
            { ArithmeticIntrinsics.Operators.IsLessThanOrEqualTo, LLVMIntPredicate.LLVMIntULE },
            { ArithmeticIntrinsics.Operators.IsLessThan, LLVMIntPredicate.LLVMIntULT }
        };

        // A mapping of floating-point arithmetic intrinsic ops to floating-point predicates.
        private static Dictionary<string, LLVMRealPredicate> floatPredicates =
            new Dictionary<string, LLVMRealPredicate>()
        {
            { ArithmeticIntrinsics.Operators.IsEqualTo, LLVMRealPredicate.LLVMRealOEQ },
            { ArithmeticIntrinsics.Operators.IsNotEqualTo, LLVMRealPredicate.LLVMRealONE },
            { ArithmeticIntrinsics.Operators.IsGreaterThanOrEqualTo, LLVMRealPredicate.LLVMRealOGE },
            { ArithmeticIntrinsics.Operators.IsGreaterThan, LLVMRealPredicate.LLVMRealOGT },
            { ArithmeticIntrinsics.Operators.IsLessThanOrEqualTo, LLVMRealPredicate.LLVMRealOLE },
            { ArithmeticIntrinsics.Operators.IsLessThan, LLVMRealPredicate.LLVMRealOLT }
        };

        // A mapping of signed arithmetic intrinsic ops to LLVM opcodes.
        private static Dictionary<string, LLVMOpcode> signedIntOps =
            new Dictionary<string, LLVMOpcode>()
        {
            { ArithmeticIntrinsics.Operators.Add, LLVMOpcode.LLVMAdd },
            { ArithmeticIntrinsics.Operators.Subtract, LLVMOpcode.LLVMSub },
            { ArithmeticIntrinsics.Operators.Multiply, LLVMOpcode.LLVMMul },
            { ArithmeticIntrinsics.Operators.Divide, LLVMOpcode.LLVMSDiv },
            { ArithmeticIntrinsics.Operators.Remainder, LLVMOpcode.LLVMSRem },
            { ArithmeticIntrinsics.Operators.And, LLVMOpcode.LLVMAnd },
            { ArithmeticIntrinsics.Operators.Or, LLVMOpcode.LLVMOr },
            { ArithmeticIntrinsics.Operators.Xor, LLVMOpcode.LLVMXor },
            { ArithmeticIntrinsics.Operators.LeftShift, LLVMOpcode.LLVMShl },
            { ArithmeticIntrinsics.Operators.RightShift, LLVMOpcode.LLVMAShr }
        };

        // A mapping of unsigned arithmetic intrinsic ops to LLVM opcodes.
        private static Dictionary<string, LLVMOpcode> unsignedIntOps =
            new Dictionary<string, LLVMOpcode>()
        {
            { ArithmeticIntrinsics.Operators.Add, LLVMOpcode.LLVMAdd },
            { ArithmeticIntrinsics.Operators.Subtract, LLVMOpcode.LLVMSub },
            { ArithmeticIntrinsics.Operators.Multiply, LLVMOpcode.LLVMMul },
            { ArithmeticIntrinsics.Operators.Divide, LLVMOpcode.LLVMUDiv },
            { ArithmeticIntrinsics.Operators.Remainder, LLVMOpcode.LLVMURem },
            { ArithmeticIntrinsics.Operators.And, LLVMOpcode.LLVMAnd },
            { ArithmeticIntrinsics.Operators.Or, LLVMOpcode.LLVMOr },
            { ArithmeticIntrinsics.Operators.Xor, LLVMOpcode.LLVMXor },
            { ArithmeticIntrinsics.Operators.LeftShift, LLVMOpcode.LLVMShl },
            { ArithmeticIntrinsics.Operators.RightShift, LLVMOpcode.LLVMLShr }
        };

        // A mapping of floating-point intrinsic ops to LLVM opcodes.
        private static Dictionary<string, LLVMOpcode> floatOps =
            new Dictionary<string, LLVMOpcode>()
        {
            { ArithmeticIntrinsics.Operators.Add, LLVMOpcode.LLVMFAdd },
            { ArithmeticIntrinsics.Operators.Subtract, LLVMOpcode.LLVMFSub },
            { ArithmeticIntrinsics.Operators.Multiply, LLVMOpcode.LLVMFMul },
            { ArithmeticIntrinsics.Operators.Divide, LLVMOpcode.LLVMFDiv },
            { ArithmeticIntrinsics.Operators.Remainder, LLVMOpcode.LLVMFRem }
        };
    }
}
