using Flame.TypeSystem;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Describes an update to a flow graph.
    /// </summary>
    public abstract class FlowGraphUpdate
    {
        internal FlowGraphUpdate()
        { }
    }

    /// <summary>
    /// A flow graph update at the instruction level:
    /// the insertion, deletion or replacement of an instruction.
    /// These updates don't affect the control flow graph itself.
    /// </summary>
    public abstract class InstructionUpdate : FlowGraphUpdate
    {
        internal InstructionUpdate(ValueTag tag)
        {
            this.Tag = tag;
        }

        /// <summary>
        /// Gets the tag of the instruction that is updated.
        /// </summary>
        /// <value>The tag of an instruction.</value>
        public ValueTag Tag { get; private set; }
    }

    /// <summary>
    /// A flow graph update that inserts an instruction.
    /// </summary>
    public sealed class AddInstructionUpdate : InstructionUpdate
    {
        internal AddInstructionUpdate(
            ValueTag tag,
            Instruction instruction)
            : base(tag)
        {
            this.Instruction = instruction;
        }

        /// <summary>
        /// Gets the instruction that is added to the graph.
        /// </summary>
        /// <value>The instruction.</value>
        public Instruction Instruction { get; private set; }
    }

    /// <summary>
    /// A flow graph update that replaces an instruction.
    /// </summary>
    public sealed class ReplaceInstructionUpdate : InstructionUpdate
    {
        internal ReplaceInstructionUpdate(
            ValueTag tag,
            Instruction instruction)
            : base(tag)
        {
            this.Instruction = instruction;
        }

        /// <summary>
        /// Gets the new instruction that replaces the old one.
        /// </summary>
        /// <value>The new instruction.</value>
        public Instruction Instruction { get; private set; }
    }

    /// <summary>
    /// A flow graph update that removes an instruction.
    /// </summary>
    public sealed class RemoveInstructionUpdate : InstructionUpdate
    {
        internal RemoveInstructionUpdate(ValueTag tag)
            : base(tag)
        { }
    }

    /// <summary>
    /// A flow graph update at the basic block level:
    /// the insertion, deletion or modification of a basic block.
    /// </summary>
    public abstract class BasicBlockUpdate : FlowGraphUpdate
    {
        internal BasicBlockUpdate(BasicBlockTag tag)
        {
            this.Tag = tag;
        }

        /// <summary>
        /// Gets the tag of the block that is updated.
        /// </summary>
        /// <value>The tag of a basic block.</value>
        public BasicBlockTag Tag { get; private set; }
    }

    /// <summary>
    /// A flow graph update that adds a basic block to the flow graph.
    /// </summary>
    public sealed class AddBasicBlockUpdate : BasicBlockUpdate
    {
        internal AddBasicBlockUpdate(BasicBlockTag tag)
            : base(tag)
        { }
    }

    /// <summary>
    /// A flow graph update that removes a basic block from the flow graph.
    /// </summary>
    public sealed class RemoveBasicBlockUpdate : BasicBlockUpdate
    {
        internal RemoveBasicBlockUpdate(BasicBlockTag tag)
            : base(tag)
        { }
    }

    /// <summary>
    /// A flow graph update that sets the graph's entry point to a new block.
    /// </summary>
    public sealed class SetEntryPointUpdate : BasicBlockUpdate
    {
        internal SetEntryPointUpdate(BasicBlockTag tag)
            : base(tag)
        { }
    }

    /// <summary>
    /// A flow graph update that sets the parameters of a basic block.
    /// </summary>
    public sealed class BasicBlockParametersUpdate : BasicBlockUpdate
    {
        internal BasicBlockParametersUpdate(BasicBlockTag tag)
            : base(tag)
        { }
    }

    /// <summary>
    /// A flow graph update that sets the outgoing flow of a basic block.
    /// </summary>
    public sealed class BasicBlockFlowUpdate : BasicBlockUpdate
    {
        internal BasicBlockFlowUpdate(BasicBlockTag tag)
            : base(tag)
        { }
    }

    /// <summary>
    /// A flow graph update that applies a mapping to every member
    /// in the flow graph.
    /// </summary>
    public sealed class MapMembersUpdate : FlowGraphUpdate
    {
        internal MapMembersUpdate(MemberMapping mapping)
        {
            this.Mapping = mapping;
        }

        /// <summary>
        /// Gets the member mapping that is applied to the flow graph.
        /// </summary>
        /// <value>A member mapping.</value>
        public MemberMapping Mapping { get; private set; }
    }
}
