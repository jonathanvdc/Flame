using System;

namespace Flame.Llvm.Emit
{
    public unsafe sealed class IRBuilder : IDisposable
    {
        public IRBuilder(LLVMContextRef context)
        {
            Builder = LLVM.CreateBuilderInContext(context);
        }

        private LLVMBuilderRef Builder { get; }

        public void Dispose()
        {
            LLVM.DisposeBuilder(Builder);
        }

        public void PositionBuilderAtEnd(LLVMBasicBlockRef block)
        {
            LLVM.PositionBuilderAtEnd(Builder, block);
        }

        public LLVMBasicBlockRef GetInsertBlock()
        {
            return LLVM.GetInsertBlock(Builder);
        }

        public LLVMValueRef CreatePhi(LLVMTypeRef type, string name)
        {
            return Builder.BuildPhi(type, name);
        }

        public LLVMValueRef CreateLoad(LLVMTypeRef type, LLVMValueRef pointer, string name)
        {
            return Builder.BuildLoad2(type, pointer, name);
        }

        public LLVMValueRef CreateStore(LLVMValueRef value, LLVMValueRef pointer)
        {
            return Builder.BuildStore(value, pointer);
        }

        public LLVMValueRef CreateAlloca(LLVMTypeRef type, string name)
        {
            return Builder.BuildAlloca(type, name);
        }

        public LLVMValueRef CreateArrayAlloca(LLVMTypeRef type, LLVMValueRef value, string name)
        {
            return Builder.BuildArrayAlloca(type, value, name);
        }

        public LLVMValueRef CreateArrayMalloc(LLVMTypeRef type, LLVMValueRef value, string name)
        {
            return Builder.BuildArrayMalloc(type, value, name);
        }

        public LLVMValueRef CreateBitCast(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildBitCast(value, type, name);
        }

        public LLVMValueRef CreateStructGEP(LLVMTypeRef pointeeType, LLVMValueRef pointer, uint index, string name)
        {
            return Builder.BuildStructGEP2(pointeeType, pointer, index, name);
        }

        public LLVMValueRef CreateGEP(LLVMTypeRef pointeeType, LLVMValueRef pointer, LLVMValueRef[] indices, string name)
        {
            return Builder.BuildGEP2(pointeeType, pointer, indices, name);
        }

        public LLVMValueRef CreateCall(LLVMTypeRef signature, LLVMValueRef function, LLVMValueRef[] args, string name)
        {
            return Builder.BuildCall2(signature, function, args, name);
        }

        public LLVMValueRef CreateRet(LLVMValueRef value)
        {
            return Builder.BuildRet(value);
        }

        public LLVMValueRef CreateRetVoid()
        {
            return Builder.BuildRetVoid();
        }

        public LLVMValueRef CreateBr(LLVMBasicBlockRef dest)
        {
            return Builder.BuildBr(dest);
        }

        public LLVMValueRef CreateCondBr(LLVMValueRef condition, LLVMBasicBlockRef thenBlock, LLVMBasicBlockRef elseBlock)
        {
            return Builder.BuildCondBr(condition, thenBlock, elseBlock);
        }

        public LLVMValueRef CreateSwitch(LLVMValueRef value, LLVMBasicBlockRef elseBlock, uint numCases)
        {
            return Builder.BuildSwitch(value, elseBlock, numCases);
        }

        public LLVMValueRef CreateAdd(LLVMValueRef lhs, LLVMValueRef rhs, string name)
        {
            return Builder.BuildAdd(lhs, rhs, name);
        }

        public LLVMValueRef CreateMul(LLVMValueRef lhs, LLVMValueRef rhs, string name)
        {
            return Builder.BuildMul(lhs, rhs, name);
        }

        public LLVMValueRef CreateZExt(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildZExt(value, type, name);
        }

        public LLVMValueRef CreateTrunc(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildTrunc(value, type, name);
        }

        public LLVMValueRef CreateSExt(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildSExt(value, type, name);
        }

        public LLVMValueRef CreateIntCast(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildIntCast(value, type, name);
        }

        public LLVMValueRef CreatePtrToInt(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildPtrToInt(value, type, name);
        }

        public LLVMValueRef CreateIntToPtr(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildIntToPtr(value, type, name);
        }

        public LLVMValueRef CreateBinOp(LLVMOpcode opcode, LLVMValueRef lhs, LLVMValueRef rhs, string name)
        {
            return Builder.BuildBinOp(opcode, lhs, rhs, name);
        }

        public LLVMValueRef CreateURem(LLVMValueRef lhs, LLVMValueRef rhs, string name)
        {
            return Builder.BuildBinOp(LLVMOpcode.LLVMURem, lhs, rhs, name);
        }

        public LLVMValueRef CreateICmp(LLVMIntPredicate predicate, LLVMValueRef lhs, LLVMValueRef rhs, string name)
        {
            return Builder.BuildICmp(predicate, lhs, rhs, name);
        }

        public LLVMValueRef CreateFCmp(LLVMRealPredicate predicate, LLVMValueRef lhs, LLVMValueRef rhs, string name)
        {
            return Builder.BuildFCmp(predicate, lhs, rhs, name);
        }

        public LLVMValueRef CreateSelect(LLVMValueRef condition, LLVMValueRef thenValue, LLVMValueRef elseValue, string name)
        {
            return Builder.BuildSelect(condition, thenValue, elseValue, name);
        }

        public LLVMValueRef CreateIsNull(LLVMValueRef value, string name)
        {
            return Builder.BuildIsNull(value, name);
        }

        public LLVMValueRef CreateFence(LLVMAtomicOrdering ordering, bool singleThread, string name)
        {
            return Builder.BuildFence(ordering, singleThread, name);
        }

        public LLVMValueRef CreateAtomicRMW(LLVMAtomicRMWBinOp op, LLVMValueRef pointer, LLVMValueRef value, LLVMAtomicOrdering ordering, bool singleThread)
        {
            return Builder.BuildAtomicRMW(op, pointer, value, ordering, singleThread);
        }

        public LLVMValueRef CreateAtomicCmpXchg(
            LLVMValueRef pointer,
            LLVMValueRef comparand,
            LLVMValueRef value,
            LLVMAtomicOrdering successOrdering,
            LLVMAtomicOrdering failureOrdering,
            bool singleThread)
        {
            return new LLVMValueRef(
                (IntPtr)LLVM.BuildAtomicCmpXchg(
                    Builder,
                    pointer,
                    comparand,
                    value,
                    successOrdering,
                    failureOrdering,
                    singleThread ? 1 : 0));
        }

        public LLVMValueRef CreateExtractValue(LLVMValueRef aggregateValue, uint index, string name)
        {
            return Builder.BuildExtractValue(aggregateValue, index, name);
        }

        public LLVMValueRef CreateFPToSI(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildFPToSI(value, type, name);
        }

        public LLVMValueRef CreateFPToUI(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildFPToUI(value, type, name);
        }

        public LLVMValueRef CreateSIToFP(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildSIToFP(value, type, name);
        }

        public LLVMValueRef CreateUIToFP(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildUIToFP(value, type, name);
        }

        public LLVMValueRef CreateFPCast(LLVMValueRef value, LLVMTypeRef type, string name)
        {
            return Builder.BuildFPCast(value, type, name);
        }

        public LLVMValueRef CreateUnreachable()
        {
            return Builder.BuildUnreachable();
        }
    }
}
