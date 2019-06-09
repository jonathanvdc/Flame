using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
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
            var entryThunk = Function.AppendBasicBlock(body.Implementation.EntryPoint.Tag.Name + ".thunk");

            // Create blocks.
            foreach (var block in body.Implementation.BasicBlocks)
            {
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

            // Jump to the entry point.
            FillJumpThunk(entryThunk, body.Implementation.EntryPoint, Function.GetParams());
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
                emittedValues[instruction] = Emit(instruction.Instruction, builder, instruction.Tag.Name);
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
            else if (proto is ReinterpretCastPrototype)
            {
                return builder.CreateBitCast(
                    Get(instruction.Arguments[0]),
                    Module.ImportType(instruction.ResultType),
                    name);
            }
            else
            {
                throw new NotSupportedException($"Unsupported instruction prototype '{proto}'.");
            }
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
                builder.CreateBr(
                    CreateJumpThunk(
                        graph.GetBasicBlock(branch.Target),
                        branch.Arguments.Select(arg => Get(arg.ValueOrNull)).ToArray()));
            }
            else
            {
                throw new NotSupportedException($"Unsupported block flow '{flow}'.");
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
    }
}
