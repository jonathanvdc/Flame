using System;
using System.Collections.Generic;
using System.Threading;

namespace Flame.Compiler
{
    /// <summary>
    /// A named instruction in the context of a control-flow graph.
    /// </summary>
    public sealed class NamedInstruction : IEquatable<NamedInstruction>
    {
        internal NamedInstruction(
            BasicBlock block, ValueTag tag, Instruction instruction)
        {
            this.Block = block;
            this.Tag = tag;
            this.Instruction = instruction;
            this.instrIndexValue = -1;
        }

        internal NamedInstruction(
            BasicBlock block,
            ValueTag tag,
            Instruction instruction,
            int instructionIndex)
        {
            this.Block = block;
            this.Tag = tag;
            this.Instruction = instruction;
            this.instrIndexValue = instructionIndex;
        }

        /// <summary>
        /// Gets the tag assigned to this instruction.
        /// </summary>
        /// <returns>The instruction's tag.</returns>
        public ValueTag Tag { get; private set; }

        /// <summary>
        /// Gets the basic block that defines this named instruction.
        /// </summary>
        /// <returns>The basic block.</returns>
        public BasicBlock Block { get; private set; }

        /// <summary>
        /// Gets the actual instruction behind this instruction selector.
        /// </summary>
        /// <returns>The instruction.</returns>
        public Instruction Instruction { get; private set; }

        /// <summary>
        /// Gets the named instruction's result type.
        /// </summary>
        public IType ResultType => Instruction.ResultType;

        /// <summary>
        /// Gets the named instruction's prototype.
        /// </summary>
        public InstructionPrototype Prototype => Instruction.Prototype;

        /// <summary>
        /// Gets the list of values this instruction takes as arguments.
        /// </summary>
        public IReadOnlyList<ValueTag> Arguments => Instruction.Arguments;

        private int instrIndexValue;

        /// <summary>
        /// Gets the index of this instruction in the defining block's
        /// list of instructions.
        /// </summary>
        /// <returns>The instruction index.</returns>
        public int InstructionIndex
        {
            get
            {
                if (instrIndexValue < 0)
                {
                    instrIndexValue = Block.InstructionTags.IndexOf(Tag);
                    Interlocked.MemoryBarrier();
                }
                return instrIndexValue;
            }
        }

        /// <summary>
        /// Gets the previous instruction in the basic block that defines
        /// this instruction. Returns null if there is no such instruction.
        /// </summary>
        /// <returns>The previous instruction or null.</returns>
        public NamedInstruction PreviousInstructionOrNull
        {
            get
            {
                int prevIndex = InstructionIndex - 1;
                if (prevIndex < 0)
                {
                    return null;
                }
                else
                {
                    return Block.Graph.GetInstruction(Block.InstructionTags[prevIndex]);
                }
            }
        }

        /// <summary>
        /// Gets the next instruction in the basic block that defines
        /// this instruction. Returns null if there is no such instruction.
        /// </summary>
        /// <returns>The next instruction or null.</returns>
        public NamedInstruction NextInstructionOrNull
        {
            get
            {
                int nextIndex = InstructionIndex + 1;
                if (nextIndex == Block.InstructionTags.Count)
                {
                    return null;
                }
                else
                {
                    return Block.Graph.GetInstruction(Block.InstructionTags[nextIndex]);
                }
            }
        }

        /// <summary>
        /// Replaces this instruction with another instruction. Returns
        /// the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">
        /// The other instruction to replace this instruction with.
        /// </param>
        /// <returns>
        /// A new instruction in a new control-flow graph.
        /// </returns>
        public NamedInstruction ReplaceInstruction(Instruction instruction)
        {
            return Block.Graph.ReplaceInstruction(Tag, instruction);
        }

        /// <summary>
        /// Replaces this instruction with a control-flow graph that implements
        /// this instruction. The arity of the control-flow graph's entry point
        /// block must match this instruction's arity.
        /// </summary>
        /// <param name="implementation">
        /// A control-flow graph that implements the instruction.
        /// </param>
        /// <returns>
        /// A new instruction in a new control-flow graph.
        /// </returns>
        public NamedInstruction ReplaceInstruction(FlowGraph implementation)
        {
            var builder = Block.Graph.ToBuilder().GetInstruction(this);
            builder.ReplaceInstruction(implementation);
            return builder.ToImmutable();
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public NamedInstruction InsertBefore(Instruction instruction, ValueTag tag)
        {
            return Block.Graph.InsertInstructionInBasicBlock(
                Block.Tag,
                instruction,
                tag,
                InstructionIndex);
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public NamedInstruction InsertBefore(Instruction instruction, string name)
        {
            return InsertBefore(instruction, new ValueTag(name));
        }

        /// <summary>
        /// Inserts a particular instruction just before this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public NamedInstruction InsertBefore(Instruction instruction)
        {
            return InsertBefore(instruction, "");
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public NamedInstruction InsertAfter(Instruction instruction, ValueTag tag)
        {
            return Block.Graph.InsertInstructionInBasicBlock(
                Block.Tag,
                instruction,
                tag,
                InstructionIndex + 1);
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name for the instruction.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public NamedInstruction InsertAfter(Instruction instruction, string name)
        {
            return InsertAfter(instruction, new ValueTag(name));
        }

        /// <summary>
        /// Inserts a particular instruction just after this instruction.
        /// Returns the new instruction in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction in a new control-flow graph.</returns>
        public NamedInstruction InsertAfter(Instruction instruction)
        {
            return InsertAfter(instruction, "");
        }

        /// <summary>
        /// Tests if this named instruction is the same instruction
        /// as another named instruction.
        /// </summary>
        /// <param name="other">The other named instruction.</param>
        /// <returns>
        /// <c>true</c> if this named instruction is the same as
        /// the other named instruction; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(NamedInstruction other)
        {
            return Tag == other.Tag && Block.Graph == other.Block.Graph;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is NamedInstruction
                && Equals((NamedInstruction)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Block.Graph.GetHashCode() << 16) ^ Tag.GetHashCode();
        }

        /// <summary>
        /// Implicitly converts an instruction to its tag.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to convert.
        /// </param>
        public static implicit operator ValueTag(NamedInstruction instruction)
        {
            return instruction.Tag;
        }
    }
}
