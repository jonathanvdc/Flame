using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Flow;

namespace Flame.Compiler
{
    /// <summary>
    /// An instruction in a mutable control-flow graph builder.
    /// </summary>
    public sealed class InstructionBuilder : IEquatable<InstructionBuilder>
    {
        /// <summary>
        /// Creates an instruction builder from a graph and a tag.
        /// </summary>
        /// <param name="graph">The instruction builder's defining graph.</param>
        /// <param name="tag">The instruction's tag.</param>
        internal InstructionBuilder(FlowGraphBuilder graph, ValueTag tag)
        {
            this.Graph = graph;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets this instruction's tag.
        /// </summary>
        /// <returns>The instruction's tag.</returns>
        public ValueTag Tag { get; private set; }

        /// <summary>
        /// Gets the control-flow graph builder that defines this
        /// instruction.
        /// </summary>
        /// <returns>The control-flow graph builder.</returns>
        public FlowGraphBuilder Graph { get; private set; }

        /// <summary>
        /// Tells if this instruction builder is still valid, that is,
        /// it has not been removed from its control-flow graph builder's
        /// set of instructions.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instruction builder is still valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid => Graph.ContainsInstruction(Tag);

        private SelectedInstruction ImmutableInstruction =>
            Graph.ImmutableGraph.GetInstruction(Tag);

        /// <summary>
        /// Gets the actual instruction behind this instruction selector.
        /// </summary>
        /// <returns>The instruction.</returns>
        public Instruction Instruction
        {
            get
            {
                return ImmutableInstruction.Instruction;
            }
            set
            {
                Graph.ImmutableGraph =
                    ImmutableInstruction.ReplaceInstruction(value).Block.Graph;
            }
        }

        /// <summary>
        /// Gets the selected instruction's result type.
        /// </summary>
        public IType ResultType => Instruction.ResultType;

        /// <summary>
        /// Gets the selected instruction's prototype.
        /// </summary>
        public InstructionPrototype Prototype => Instruction.Prototype;

        /// <summary>
        /// Gets the index of this instruction in the defining block's
        /// list of instructions.
        /// </summary>
        /// <returns>The instruction index.</returns>
        public int InstructionIndex => ImmutableInstruction.InstructionIndex;

        /// <summary>
        /// Gets an immutable version of this instruction, that is,
        /// this instruction but selected in an immutable version of the
        /// current state of the IR builder.
        /// </summary>
        /// <returns>An immutable version.</returns>
        public SelectedInstruction ToImmutable()
        {
            return ImmutableInstruction;
        }

        /// <summary>
        /// Gets the previous instruction in the basic block that defines
        /// this instruction. Returns null if there is no such instruction.
        /// </summary>
        /// <returns>The previous instruction or null.</returns>
        public InstructionBuilder PreviousInstructionOrNull
        {
            get
            {
                var prevInsn = ImmutableInstruction.PreviousInstructionOrNull;
                return prevInsn == null ? null : Graph.GetInstruction(prevInsn.Tag);
            }
        }

        /// <summary>
        /// Gets the next instruction in the basic block that defines
        /// this instruction. Returns null if there is no such instruction.
        /// </summary>
        /// <returns>The next instruction or null.</returns>
        public InstructionBuilder NextInstructionOrNull
        {
            get
            {
                var nextInsn = ImmutableInstruction.NextInstructionOrNull;
                return nextInsn == null ? null : Graph.GetInstruction(nextInsn.Tag);
            }
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The tag to assign to the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertBefore(Instruction instruction, ValueTag tag)
        {
            var selInsn = ImmutableInstruction.InsertBefore(instruction, tag);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertBefore(Instruction instruction, string name)
        {
            var selInsn = ImmutableInstruction.InsertBefore(instruction, name);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertBefore(Instruction instruction)
        {
            return InsertBefore(instruction, "");
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The tag to assign to the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertAfter(Instruction instruction, ValueTag tag)
        {
            var selInsn = ImmutableInstruction.InsertAfter(instruction, tag);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertAfter(Instruction instruction, string name)
        {
            var selInsn = ImmutableInstruction.InsertAfter(instruction, name);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction.</returns>
        public InstructionBuilder InsertAfter(Instruction instruction)
        {
            return InsertAfter(instruction, "");
        }

        /// <summary>
        /// Moves this instruction from its current location to a
        /// particular position in a block.
        /// </summary>
        /// <param name="index">
        /// The position in <paramref name="block"/> at which to insert
        /// this instruction.
        /// </param>
        /// <param name="block">
        /// The block to move this instruction to.
        /// </param>
        public void MoveTo(int index, BasicBlockTag block)
        {
            var data = this.Instruction;
            var target = Graph.GetBasicBlock(block);
            Graph.RemoveInstruction(Tag);
            target.InsertInstruction(index, data, Tag);
        }

        /// <summary>
        /// Moves this instruction from its current location to a
        /// the end of a basic block.
        /// </summary>
        /// <param name="block">
        /// The block to move this instruction to.
        /// </param>
        public void MoveTo(BasicBlockTag block)
        {
            var data = this.Instruction;
            var target = Graph.GetBasicBlock(block);
            Graph.RemoveInstruction(Tag);
            target.AppendInstruction(data, Tag);
        }

        /// <summary>
        /// Replaces this instruction with a control-flow graph that implements
        /// this instruction. The arity of the control-flow graph's entry point
        /// block must match this instruction's arity.
        /// </summary>
        /// <param name="implementation">
        /// A control-flow graph that implements the instruction.
        /// </param>
        public void ReplaceInstruction(FlowGraph implementation)
        {
            // The first thing we want to do is compose a mapping of
            // value tags in `implementation` to value tags in this
            // control-flow graph.
            var valueRenameMap = new Dictionary<ValueTag, ValueTag>();
            foreach (var insn in implementation.Instructions)
            {
                valueRenameMap[insn] = new ValueTag(insn.Tag.Name);
            }

            if (implementation.EntryPoint.Flow is ReturnFlow)
            {
                // In the likely case where the implementation consists of a
                // basic block that immediately returns a value, we will insert
                // the block's instructions just before this instruction and set
                // this instruction to the return value.
                // Also take care to rename instructions as we step through the
                // basic block: we don't want two different expansions of the same
                // block to conflict.

                // Rename parameters.
                var parameters = implementation.EntryPoint.Parameters;
                for (int i = 0; i < parameters.Count; i++)
                {
                    valueRenameMap[parameters[i].Tag] = Instruction.Arguments[i];
                }

                // Insert instructions.
                foreach (var insn in implementation.EntryPoint.Instructions)
                {
                    InsertBefore(
                        insn.Instruction.MapArguments(valueRenameMap),
                        valueRenameMap[insn]);
                }

                // Copy the return value.
                var returnFlow = (ReturnFlow)implementation.EntryPoint.Flow;
                Instruction = returnFlow.ReturnValue.MapArguments(valueRenameMap);
            }
            else
            {
                // Otherwise, we will just copy the entire control-flow graph
                // into this control-flow graph and cut the current basic block
                // in two.

                // Create a continuation block, which represents the remainder
                // of this basic block, after `implementation` has run.
                var continuationBlock = Graph.AddBasicBlock();
                var resultParam = new BlockParameter(ResultType);
                continuationBlock.AppendParameter(resultParam);

                // Split the parent basic block in two, copying all instructions
                // after this one to the continuation block. Include this instruction
                // as well because we'll turn it into a copy that runs in the continuation.
                MoveTo(continuationBlock);
                var parentBlock = Graph.GetValueParent(this);
                foreach (var insn in parentBlock.Instructions.Skip(InstructionIndex + 1).ToArray())
                {
                    insn.MoveTo(continuationBlock);
                }

                // Populate a basic block rename mapping.
                var blockMap = new Dictionary<BasicBlockTag, BasicBlockBuilder>();
                var blockRenameMap = new Dictionary<BasicBlockTag, BasicBlockTag>();
                foreach (var block in implementation.BasicBlocks)
                {
                    // Add a basic block.
                    var newBlock = Graph.AddBasicBlock(block.Tag.Name);
                    blockMap[block] = newBlock;
                    blockRenameMap[block] = newBlock;

                    // Also handle parameters here.
                    foreach (var param in block.Parameters)
                    {
                        var newParam = new BlockParameter(param.Type, param.Tag.Name);
                        newBlock.AppendParameter(newParam);
                        valueRenameMap[param.Tag] = newParam.Tag;
                    }
                }

                // Copy basic block instructions and flow.
                foreach (var block in implementation.BasicBlocks)
                {
                    var newBlock = blockMap[block];
                    // Copy the block's instructions.
                    foreach (var insn in block.Instructions)
                    {
                        newBlock.AppendInstruction(
                            insn.Instruction.MapArguments(valueRenameMap),
                            valueRenameMap[insn]);
                    }

                    // If the block ends in 'return' flow, then we want to
                    // turn that return into a jump to the continuation.
                    if (block.Flow is ReturnFlow)
                    {
                        var returnFlow = (ReturnFlow)block.Flow;
                        ValueTag resultTag = newBlock.AppendInstruction(
                            returnFlow.ReturnValue.MapArguments(valueRenameMap));
                        newBlock.Flow = new JumpFlow(continuationBlock, new[] { resultTag });
                    }
                    else
                    {
                        newBlock.Flow = block.Flow
                            .MapValues(valueRenameMap)
                            .MapBlocks(blockRenameMap);
                    }
                }

                // Copy the parent basic block's flow to the continuation block.
                continuationBlock.Flow = parentBlock.Flow;

                // Set the parent basic block's flow to a jump to `implementation`'s
                // entry point.
                parentBlock.Flow = new JumpFlow(
                    blockRenameMap[implementation.EntryPointTag],
                    Instruction.Arguments);

                // Replace this instruction with a copy of the result parameter.
                Instruction = Instruction.CreateCopy(ResultType, resultParam.Tag);
            }
        }

        /// <summary>
        /// Tests if this instruction builder is the same instruction
        /// as another instruction builder.
        /// </summary>
        /// <param name="other">The other instruction builder.</param>
        /// <returns>
        /// <c>true</c> if this instruction builder is the same as
        /// the other instruction builder; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(InstructionBuilder other)
        {
            return Tag == other.Tag && Graph == other.Graph;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is InstructionBuilder
                && Equals((InstructionBuilder)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Graph.GetHashCode() << 16) ^ Tag.GetHashCode();
        }

        /// <summary>
        /// Implicitly converts an instruction to its tag.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to convert.
        /// </param>
        public static implicit operator ValueTag(InstructionBuilder instruction)
        {
            return instruction.Tag;
        }
    }
}
