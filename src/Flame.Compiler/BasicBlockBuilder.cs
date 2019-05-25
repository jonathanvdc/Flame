using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flame.Compiler
{
    /// <summary>
    /// A basic block in a mutable control-flow graph builder.
    /// </summary>
    public sealed class BasicBlockBuilder : IEquatable<BasicBlockBuilder>
    {
        /// <summary>
        /// Creates a basic block builder from a graph and a tag.
        /// </summary>
        /// <param name="graph">The basic block builder's defining graph.</param>
        /// <param name="tag">The basic block's tag.</param>
        internal BasicBlockBuilder(FlowGraphBuilder graph, BasicBlockTag tag)
        {
            this.Graph = graph;
            this.Tag = tag;
        }

        /// <summary>
        /// Gets this basic block's tag.
        /// </summary>
        /// <returns>The basic block's tag.</returns>
        public BasicBlockTag Tag { get; private set; }

        /// <summary>
        /// Gets the control-flow graph builder that defines this
        /// basic block.
        /// </summary>
        /// <returns>The control-flow graph builder.</returns>
        public FlowGraphBuilder Graph { get; private set; }

        /// <summary>
        /// Tells if this basic block builder is still valid, that is,
        /// it has not been removed from its control-flow graph builder's
        /// set of basic blocks.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this basic block builder is still valid; otherwise, <c>false</c>.
        /// </returns>
        public bool IsValid => Graph.ContainsBasicBlock(Tag);

        private BasicBlock ImmutableBlock => Graph.ImmutableGraph.GetBasicBlock(Tag);

        /// <summary>
        /// Gets or sets this basic block's list of parameters.
        /// </summary>
        /// <returns>The basic block's parameters.</returns>
        public ImmutableList<BlockParameter> Parameters
        {
            get
            {
                return ImmutableBlock.Parameters;
            }
            set
            {
                Graph.ImmutableGraph = ImmutableBlock.WithParameters(value).Graph;
            }
        }

        /// <summary>
        /// Gets a sequence of all parameter tags defined by this basic block.
        /// </summary>
        /// <returns>The basic block's parameter tags.</returns>
        public IEnumerable<ValueTag> ParameterTags => ImmutableBlock.ParameterTags;

        /// <summary>
        /// Gets the list of all instruction tags in this basic block.
        /// </summary>
        /// <returns>The list of all instruction tags.</returns>
        public ImmutableList<ValueTag> InstructionTags => ImmutableBlock.InstructionTags;

        /// <summary>
        /// Gets the list of all named instructions in this basic block.
        /// </summary>
        /// <returns>A sequence containing all named instructions.</returns>
        public IEnumerable<NamedInstructionBuilder> NamedInstructions =>
            InstructionTags.Select(Graph.GetInstruction);

        /// <summary>
        /// Tells if this block is the graph's entry point.
        /// </summary>
        public bool IsEntryPoint => ImmutableBlock.IsEntryPoint;

        /// <summary>
        /// Gets or sets the control flow at the end of this basic block.
        /// </summary>
        /// <returns>The end-of-block control flow.</returns>
        public BlockFlow Flow
        {
            get
            {
                return ImmutableBlock.Flow;
            }
            set
            {
                Graph.ImmutableGraph = ImmutableBlock.WithFlow(value).Graph;
            }
        }

        /// <summary>
        /// Removes the instruction with a particular tag from
        /// this basic block.
        /// </summary>
        /// <param name="tag">The tag of the instruction to remove.</param>s
        public void RemoveInstruction(ValueTag tag)
        {
            Graph.ImmutableGraph = ImmutableBlock.RemoveInstruction(tag).Graph;
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>The appended instruction.</returns>
        public NamedInstructionBuilder AppendInstruction(Instruction instruction, ValueTag tag)
        {
            var selInsn = ImmutableBlock.AppendInstruction(instruction, tag);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <param name="name">The preferred name of the instruction's tag.</param>
        /// <returns>The appended instruction.</returns>
        public NamedInstructionBuilder AppendInstruction(Instruction instruction, string name)
        {
            return AppendInstruction(instruction, new ValueTag(name));
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="instruction">The instruction to append.</param>
        /// <returns>The appended instruction.</returns>
        public NamedInstructionBuilder AppendInstruction(Instruction instruction)
        {
            return AppendInstruction(instruction, new ValueTag());
        }

        /// <summary>
        /// Inserts a new instruction into this basic block's list of instructions.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="index">The index to insert the instruction at.</param>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstructionBuilder InsertInstruction(int index, Instruction instruction, ValueTag tag)
        {
            var selInsn = ImmutableBlock.InsertInstruction(index, instruction, tag);
            Graph.ImmutableGraph = selInsn.Block.Graph;
            return Graph.GetInstruction(selInsn.Tag);
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="index">The index to insert the instruction at.</param>
        /// <param name="instruction">The instruction to insert.</param>
        /// <param name="name">The preferred name of the instruction's tag.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstructionBuilder InsertInstruction(int index, Instruction instruction, string name)
        {
            return InsertInstruction(index, instruction, new ValueTag(name));
        }

        /// <summary>
        /// Appends a new instruction to the end of this basic block.
        /// Returns the instruction builder for the inserted instruction.
        /// </summary>
        /// <param name="index">The index to insert the instruction at.</param>
        /// <param name="instruction">The instruction to insert.</param>
        /// <returns>The inserted instruction.</returns>
        public NamedInstructionBuilder InsertInstruction(int index, Instruction instruction)
        {
            return InsertInstruction(index, instruction, new ValueTag());
        }

        /// <summary>
        /// Appends a new parameter to the end of this basic block's parameter list.
        /// </summary>
        /// <param name="parameter">The parameter to append.</param>
        /// <returns>The block parameter.</returns>
        public BlockParameter AppendParameter(BlockParameter parameter)
        {
            Graph.ImmutableGraph = ImmutableBlock.AppendParameter(parameter).Graph;
            return parameter;
        }

        /// <summary>
        /// Appends a new parameter to the end of this basic block's parameter list.
        /// </summary>
        /// <param name="type">The type of the parameter to append.</param>
        /// <returns>The block parameter.</returns>
        public BlockParameter AppendParameter(IType type)
        {
            return AppendParameter(new BlockParameter(type));
        }

        /// <summary>
        /// Appends a new parameter to the end of this basic block's parameter list.
        /// </summary>
        /// <param name="type">The type of the parameter to append.</param>
        /// <param name="name">The name of the parameter to append.</param>
        /// <returns>The block parameter.</returns>
        public BlockParameter AppendParameter(IType type, string name)
        {
            return AppendParameter(new BlockParameter(type, name));
        }

        /// <summary>
        /// Appends a new parameter to the end of this basic block's parameter list.
        /// </summary>
        /// <param name="type">The type of the parameter to append.</param>
        /// <param name="tag">The tag of the parameter to append.</param>
        /// <returns>The block parameter.</returns>
        public BlockParameter AppendParameter(IType type, ValueTag tag)
        {
            return AppendParameter(new BlockParameter(type, tag));
        }

        /// <summary>
        /// Copies all instructions in a particular basic block
        /// into this basic block. All instructions defined by
        /// the block are renamed to avoid name clashes. References
        /// to the block's parameters are substituted by a list
        /// of arguments.
        /// </summary>
        /// <param name="insertionIndex">
        /// The instruction at which to insert <paramref name="block"/>'s
        /// instructions.
        /// </param>
        /// <param name="block">
        /// The block to copy instructions from.
        /// </param>
        /// <param name="arguments">
        /// The arguments to feed to the <paramref name="block"/>.
        /// </param>
        /// <returns>
        /// <paramref name="block"/>'s flow, with values renamed to
        /// inserted instruction tags.
        /// </returns>
        public BlockFlow CopyInstructionsFrom(
            int insertionIndex,
            BasicBlock block,
            IReadOnlyList<ValueTag> arguments)
        {
            // The first thing we want to do is compose a mapping of
            // value tags in the block to copy to value tags in this
            // control-flow graph.
            var valueRenameMap = new Dictionary<ValueTag, ValueTag>();
            foreach (var insn in block.NamedInstructions)
            {
                valueRenameMap[insn] = new ValueTag(insn.Tag.Name);
            }

            // Rename parameters.
            for (int i = 0; i < block.Parameters.Count; i++)
            {
                valueRenameMap[block.Parameters[i].Tag] = arguments[i];
            }

            // Insert instructions.
            foreach (var insn in block.NamedInstructions)
            {
                InsertInstruction(
                    insertionIndex,
                    insn.Instruction.MapArguments(valueRenameMap),
                    valueRenameMap[insn]);
                insertionIndex++;
            }

            // Rename values in the block flow and return it.
            return block.Flow.MapValues(valueRenameMap);
        }

        /// <summary>
        /// Tests if this basic block equals another basic block.
        /// </summary>
        /// <param name="other">The other basic block.</param>
        /// <returns>
        /// <c>true</c> if this basic block equals the other
        /// basic block; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(BasicBlockBuilder other)
        {
            return Graph == other.Graph && Tag == other.Tag;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is BasicBlockBuilder && Equals((BasicBlockBuilder)obj);
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
        public static implicit operator BasicBlockTag(BasicBlockBuilder block)
        {
            return block.Tag;
        }
    }
}
