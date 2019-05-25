using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Flow;

namespace Flame.Compiler
{
    /// <summary>
    /// A named instruction in a mutable control-flow graph builder.
    /// </summary>
    public sealed class NamedInstructionBuilder : InstructionBuilder, IEquatable<NamedInstructionBuilder>
    {
        /// <summary>
        /// Creates a named instruction builder from a graph and a tag.
        /// </summary>
        /// <param name="graph">The instruction builder's defining graph.</param>
        /// <param name="tag">The instruction's tag.</param>
        internal NamedInstructionBuilder(FlowGraphBuilder graph, ValueTag tag)
        {
            this.graph = graph;
            this.Tag = tag;
        }

        private FlowGraphBuilder graph;

        /// <summary>
        /// Gets this instruction's tag.
        /// </summary>
        /// <returns>The instruction's tag.</returns>
        public ValueTag Tag { get; private set; }

        /// <summary>
        /// Gets the control-flow graph builder that defines this
        /// instruction.
        /// </summary>
        /// <returns>A control-flow graph builder.</returns>
        public override FlowGraphBuilder Graph => graph;

        /// <summary>
        /// Gets the basic block that defines this instruction.
        /// </summary>
        /// <returns>A basic block builder.</returns>
        public override BasicBlockBuilder Block => Graph.GetValueParent(this);

        /// <summary>
        /// Tells if this instruction builder is still valid, that is,
        /// it has not been removed from its control-flow graph builder's
        /// set of instructions.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instruction builder is still valid; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsValid => Graph.ContainsInstruction(Tag);

        private NamedInstruction ImmutableInstruction =>
            Graph.ImmutableGraph.GetInstruction(Tag);

        /// <summary>
        /// Gets the actual instruction behind this instruction selector.
        /// </summary>
        /// <returns>The instruction.</returns>
        public override Instruction Instruction
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
        public NamedInstruction ToImmutable()
        {
            return ImmutableInstruction;
        }

        /// <summary>
        /// Gets the previous instruction in the basic block that defines
        /// this instruction. Returns null if there is no such instruction.
        /// </summary>
        /// <returns>The previous instruction or null.</returns>
        public NamedInstructionBuilder PreviousInstructionOrNull
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
        public NamedInstructionBuilder NextInstructionOrNull
        {
            get
            {
                var nextInsn = ImmutableInstruction.NextInstructionOrNull;
                return nextInsn == null ? null : Graph.GetInstruction(nextInsn.Tag);
            }
        }

        /// <inheritdoc/>
        public override NamedInstructionBuilder InsertBefore(Instruction instruction, ValueTag tag)
        {
            var selInsn = ImmutableInstruction.InsertBefore(instruction, tag);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the inserted instruction builder.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The tag to assign to the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstructionBuilder InsertAfter(Instruction instruction, ValueTag tag)
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
        public NamedInstructionBuilder InsertAfter(Instruction instruction, string name)
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
        public NamedInstructionBuilder InsertAfter(Instruction instruction)
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
        /// this instruction.
        /// </summary>
        /// <param name="implementation">
        /// A control-flow graph that implements the instruction.
        /// </param>
        /// <param name="arguments">
        /// A list of arguments to pass to <paramref name="implementation"/>'s
        /// entry point block.
        /// </param>
        public override void ReplaceInstruction(FlowGraph implementation, IReadOnlyList<ValueTag> arguments)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot replace an invalid instruction builder.");
            }

            if (implementation.EntryPoint.Flow is ReturnFlow)
            {
                // In the likely case where the implementation consists of a
                // basic block that immediately returns a value, we will insert
                // the block's instructions just before this instruction and set
                // this instruction to the return value.
                var returnFlow = (ReturnFlow)Block.CopyInstructionsFrom(
                    InstructionIndex,
                    implementation.EntryPoint,
                    arguments);

                // Copy the return value.
                Instruction = returnFlow.ReturnValue;
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
                var parentBlock = Graph.GetValueParent(this);
                int index = InstructionIndex;
                MoveTo(continuationBlock);
                foreach (var insn in parentBlock.NamedInstructions.Skip(index).ToArray())
                {
                    insn.MoveTo(continuationBlock);
                }

                // Include `implementation` in this graph.
                var entryTag = Graph.Include(
                    implementation,
                    (retFlow, block) =>
                    {
                        ValueTag resultTag = block.AppendInstruction(retFlow.ReturnValue);
                        return new JumpFlow(continuationBlock, new[] { resultTag });
                    });

                // Copy the parent basic block's flow to the continuation block.
                continuationBlock.Flow = parentBlock.Flow;

                // Set the parent basic block's flow to a jump to `implementation`'s
                // entry point.
                parentBlock.Flow = new JumpFlow(entryTag, arguments);

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
        public bool Equals(NamedInstructionBuilder other)
        {
            return Tag == other.Tag && Graph == other.Graph;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is NamedInstructionBuilder
                && Equals((NamedInstructionBuilder)obj);
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
        public static implicit operator ValueTag(NamedInstructionBuilder instruction)
        {
            return instruction.Tag;
        }
    }
}
