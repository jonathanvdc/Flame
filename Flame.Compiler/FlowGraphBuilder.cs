using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// A mutable view of an immutable control-flow graph.
    /// </summary>
    public sealed class FlowGraphBuilder
    {
        /// <summary>
        /// Creates a control-flow graph builder that contains
        /// only an empty entry point block.
        /// </summary>
        public FlowGraphBuilder()
            : this(new FlowGraph())
        { }

        /// <summary>
        /// Creates a control-flow graph builder from an
        /// immutable control-flow graph.
        /// </summary>
        /// <param name="graph">An immutable control-flow graph.</param>
        public FlowGraphBuilder(FlowGraph graph)
        {
            this.ImmutableGraph = graph;
        }

        /// <summary>
        /// Gets or sets the control-flow graph wrapped by this builder.
        /// </summary>
        /// <returns>The control-flow graph.</returns>
        internal FlowGraph ImmutableGraph { get; set; }

        /// <summary>
        /// Gets the tag of the entry point block.
        /// </summary>
        /// <returns>The tag of the entry point block.</returns>
        public BasicBlockTag EntryPointTag
        {
            get
            {
                return ImmutableGraph.EntryPointTag;
            }
            set
            {
                ImmutableGraph = ImmutableGraph.WithEntryPoint(value);
            }
        }

        /// <summary>
        /// Gets a sequence of all basic block tags in this control-flow graph.
        /// </summary>
        public IEnumerable<BasicBlockTag> BasicBlockTags => ImmutableGraph.BasicBlockTags;

        /// <summary>
        /// Gets a sequence of all instruction tags in this control-flow graph.
        /// </summary>
        public IEnumerable<ValueTag> InstructionTags => ImmutableGraph.InstructionTags;

        /// <summary>
        /// Adds an empty basic block to this flow-graph builder.
        /// </summary>
        /// <param name="name">The (preferred) name of the basic block's tag.</param>
        /// <returns>An empty basic block builder.</returns>
        public BasicBlockBuilder AddBasicBlock(string name)
        {
            var newBlock = ImmutableGraph.AddBasicBlock(name);
            ImmutableGraph = newBlock.Graph;
            return GetBasicBlock(newBlock.Tag);
        }

        /// <summary>
        /// Adds an empty basic block to this flow-graph builder.
        /// </summary>
        /// <returns>An empty basic block builder.</returns>
        public BasicBlockBuilder AddBasicBlock()
        {
            return AddBasicBlock("");
        }

        /// <summary>
        /// Removes the basic block with a particular tag from this
        /// control-flow graph.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        public void RemoveBasicBlock(BasicBlockTag tag)
        {
            ImmutableGraph = ImmutableGraph.RemoveBasicBlock(tag);
        }

        /// <summary>
        /// Removes a particular instruction from this control-flow graph.
        /// Returns a new control-flow graph that does not contain the
        /// instruction.
        /// </summary>
        /// <param name="instructionTag">The tag of the instruction to remove.</param>
        /// <returns>
        /// A control-flow graph that no longer contains the instruction.
        /// </returns>
        public void RemoveInstruction(ValueTag instructionTag)
        {
            ImmutableGraph = ImmutableGraph.RemoveInstruction(instructionTag);
        }

        /// <summary>
        /// Gets the basic block with a particular tag.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        /// <returns>A basic block.</returns>
        public BasicBlockBuilder GetBasicBlock(BasicBlockTag tag)
        {
            return new BasicBlockBuilder(this, tag);
        }

        /// <summary>
        /// Gets the instruction with a particular tag.
        /// </summary>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>A selected instruction.</returns>
        public InstructionBuilder GetInstruction(ValueTag tag)
        {
            return new InstructionBuilder(this, tag);
        }

        /// <summary>
        /// Checks if this control-flow graph contains a basic block
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains a basic block
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsBasicBlock(BasicBlockTag tag)
        {
            return ImmutableGraph.ContainsBasicBlock(tag);
        }

        /// <summary>
        /// Checks if this control-flow graph contains an instruction
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains an instruction
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsInstruction(ValueTag tag)
        {
            return ImmutableGraph.ContainsInstruction(tag);
        }

        /// <summary>
        /// Checks if this control-flow graph contains a basic block parameter
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">The parameter's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains a basic block parameter
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsBlockParameter(ValueTag tag)
        {
            return ImmutableGraph.ContainsBlockParameter(tag);
        }

        /// <summary>
        /// Checks if this control-flow graph contains an instruction
        /// or basic block parameter with a particular tag.
        /// </summary>
        /// <param name="tag">The value's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains a value
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsValue(ValueTag tag)
        {
            return ImmutableGraph.ContainsValue(tag);
        }

        /// <summary>
        /// Gets the type of a value in this graph.
        /// </summary>
        /// <param name="tag">The value's tag.</param>
        /// <returns>The value's type.</returns>
        public IType GetValueType(ValueTag tag)
        {
            return ImmutableGraph.GetValueType(tag);
        }

        /// <summary>
        /// Gets basic block that defines a value with a
        /// particular tag.
        /// </summary>
        /// <param name="tag">The tag of the value to look for.</param>
        /// <returns>The basic block that defines the value.</returns>
        public BasicBlockBuilder GetValueParent(ValueTag tag)
        {
            return GetBasicBlock(ImmutableGraph.GetValueParent(tag).Tag);
        }

        /// <summary>
        /// Turns this control-flow graph builder into an immutable
        /// control-flow graph.
        /// </summary>
        /// <returns>An immutable control-flow graph.</returns>
        public FlowGraph ToImmutable()
        {
            return ImmutableGraph;
        }
    }
}