using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flame.Compiler
{
    /// <summary>
    /// A basic block in a control-flow graph.
    /// </summary>
    public struct BasicBlock : IEquatable<BasicBlock>
    {
        internal BasicBlock(FlowGraph graph, BasicBlockTag tag, BasicBlockData data)
        {
            this = default(BasicBlock);
            this.Graph = graph;
            this.Tag = tag;
            this.data = data;
        }

        /// <summary>
        /// Gets the control-flow graph in which this block resides.
        /// </summary>
        /// <returns>A control-flow graph.</returns>
        public FlowGraph Graph { get; private set; }

        /// <summary>
        /// Gets this basic block's tag.
        /// </summary>
        /// <returns>The basic block's tag.</returns>
        public BasicBlockTag Tag { get; private set; }

        private BasicBlockData data;

        /// <summary>
        /// Gets this basic block's list of parameters.
        /// </summary>
        /// <returns>The basic block's parameters.</returns>
        public ImmutableList<BlockParameter> Parameters => data.Parameters;

        /// <summary>
        /// Gets the list of all instruction tags in this basic block.
        /// </summary>
        /// <returns>The list of all instruction tags.</returns>
        public ImmutableList<ValueTag> InstructionTags => data.InstructionTags;

        /// <summary>
        /// Gets a sequence of all parameter tags defined by this basic block.
        /// </summary>
        /// <returns>The basic block's parameter tags.</returns>
        public IEnumerable<ValueTag> ParameterTags =>
            data.Parameters.Select(p => p.Tag);

        /// <summary>
        /// Gets the list of all named instructions in this basic block.
        /// </summary>
        /// <returns>The list of all named instructions.</returns>
        public IEnumerable<NamedInstruction> NamedInstructions =>
            InstructionTags.Select(Graph.GetInstruction);

        /// <summary>
        /// Gets the control flow at the end of this basic block.
        /// </summary>
        /// <returns>The end-of-block control flow.</returns>
        public BlockFlow Flow => data.Flow;

        /// <summary>
        /// Tells if this block is the graph's entry point.
        /// </summary>
        public bool IsEntryPoint => Tag == Graph.EntryPointTag;

        /// <summary>
        /// Creates a new basic block in a new control-flow graph that
        /// has a particular flow.
        /// </summary>
        /// <param name="flow">The new flow.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock WithFlow(BlockFlow flow)
        {
            return Graph.UpdateBasicBlockFlow(Tag, flow);
        }

        /// <summary>
        /// Creates a new basic block in a new control-flow graph that
        /// has a particular list of parameters.
        /// </summary>
        /// <param name="parameters">The new parameters.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock WithParameters(IReadOnlyList<BlockParameter> parameters)
        {
            return WithParameters(parameters.ToImmutableList<BlockParameter>());
        }

        /// <summary>
        /// Creates a new basic block in a new control-flow graph that
        /// has a particular list of parameters.
        /// </summary>
        /// <param name="parameters">The new parameters.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock WithParameters(ImmutableList<BlockParameter> parameters)
        {
            return Graph.UpdateBasicBlockParameters(Tag, parameters);
        }

        /// <summary>
        /// Removes the instruction with a particular tag from
        /// this basic block. Returns a new basic block in a new
        /// control-flow graph.
        /// </summary>
        /// <param name="tag">The tag of the instruction to remove.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock RemoveInstruction(ValueTag tag)
        {
            ContractHelpers.Assert(
                Graph.GetValueParent(tag).Tag == this.Tag,
                "Basic block does not define the instruction being removed.");
            return Graph.RemoveInstruction(tag).GetBasicBlock(this.Tag);
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns a new basic block in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <param name="tag">The tag for the instruction.</param>
        /// <returns>The appended instruction.</returns>
        public NamedInstruction AppendInstruction(Instruction instruction, ValueTag tag)
        {
            return Graph.InsertInstructionInBasicBlock(Tag, instruction, tag, InstructionTags.Count);
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns a new basic block in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <param name="name">The preferred name of the instruction's tag.</param>
        /// <returns>The appended instruction.</returns>
        public NamedInstruction AppendInstruction(Instruction instruction, string name)
        {
            return AppendInstruction(instruction, new ValueTag(name));
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns a new basic block in a new control-flow graph.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <returns>The appended instruction.</returns>
        public NamedInstruction AppendInstruction(Instruction instruction)
        {
            return AppendInstruction(instruction, "");
        }

        /// <summary>
        /// Inserts a new instruction into this basic block's list of instructions.
        /// Returns a new basic block in a new control-flow graph.
        /// </summary>
        /// <param name="index">
        /// The index at which the instruction is to be inserted.
        /// </param>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The tag for the instruction.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstruction InsertInstruction(int index, Instruction instruction, ValueTag tag)
        {
            return Graph.InsertInstructionInBasicBlock(Tag, instruction, tag, index);
        }

        /// <summary>
        /// Inserts a new instruction into this basic block's list of instructions.
        /// Returns a new basic block in a new control-flow graph.
        /// </summary>
        /// <param name="index">
        /// The index at which the instruction is to be inserted.
        /// </param>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name of the instruction's tag.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstruction InsertInstruction(int index, Instruction instruction, string name)
        {
            return InsertInstruction(index, instruction, new ValueTag(name));
        }

        /// <summary>
        /// Inserts a new instruction into this basic block's list of instructions.
        /// Returns a new basic block in a new control-flow graph.
        /// </summary>
        /// <param name="index">
        /// The index at which the instruction is to be inserted.
        /// </param>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstruction InsertInstruction(int index, Instruction instruction)
        {
            return InsertInstruction(index, instruction, "");
        }

        /// <summary>
        /// Appends a new parameter to the end of this basic block's parameter list.
        /// Returns a new basic block in a new control-flow graph.
        /// </summary>
        /// <param name="parameter">The parameter to append.</param>
        /// <returns>A new basic block in a new control-flow graph.</returns>
        public BasicBlock AppendParameter(BlockParameter parameter)
        {
            return WithParameters(Parameters.Add(parameter));
        }

        /// <summary>
        /// Tests if this basic block equals another basic block.
        /// </summary>
        /// <param name="other">The other basic block.</param>
        /// <returns>
        /// <c>true</c> if this basic block equals the other
        /// basic block; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(BasicBlock other)
        {
            return Graph == other.Graph && Tag == other.Tag;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is BasicBlock && Equals((BasicBlock)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Graph.GetHashCode() << 16) ^ Tag.GetHashCode();
        }

        /// <summary>
        /// Implicitly converts a block to its tag.
        /// </summary>
        /// <param name="block">
        /// The block to convert.
        /// </param>
        public static implicit operator BasicBlockTag(BasicBlock block)
        {
            return block.Tag;
        }
    }
}