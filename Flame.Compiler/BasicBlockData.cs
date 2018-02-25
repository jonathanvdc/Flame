using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Compiler.Flow;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler
{
    /// <summary>
    /// The data behind a basic block in a control-flow graph.
    /// </summary>
    internal sealed class BasicBlockData
    {
        /// <summary>
        /// Creates an empty basic block.
        /// </summary>
        public BasicBlockData()
            : this(
                ImmutableList.Create<BlockParameter>(),
                ImmutableList.Create<ValueTag>(),
                UnreachableFlow.Instance)
        { }

        /// <summary>
        /// Creates a basic block from a list of parameters,
        /// a list of instructions and an end-of-block control
        /// flow instruction.
        /// </summary>
        /// <param name="parameters">
        /// The list of parameters for the basic block.
        /// </param>
        /// <param name="instructions">
        /// The list of instructions for the basic block.
        /// </param>
        /// <param name="flow">
        /// The block's end-of-block control flow.
        /// </param>
        public BasicBlockData(
            ImmutableList<BlockParameter> parameters,
            ImmutableList<ValueTag> instructions,
            BlockFlow flow)
        {
            this.Parameters = parameters;
            this.InstructionTags = instructions;
            this.Flow = flow;
        }

        /// <summary>
        /// Gets this basic block's list of parameters.
        /// </summary>
        /// <returns>The basic block's parameters.</returns>
        public ImmutableList<BlockParameter> Parameters { get; private set; }

        /// <summary>
        /// Gets the list of instructions in this basic block.
        /// </summary>
        /// <returns>The list of instructions.</returns>
        public ImmutableList<ValueTag> InstructionTags { get; private set; }

        /// <summary>
        /// Gets the control flow at the end of this basic block.
        /// </summary>
        /// <returns>The end-of-block control flow.</returns>
        public BlockFlow Flow { get; private set; }

        /// <summary>
        /// Applies a member mapping to this basic block.
        /// </summary>
        /// <param name="mapping">A member mapping.</param>
        /// <returns>A transformed basic block.</returns>
        public BasicBlockData Map(MemberMapping mapping)
        {
            var newParameters = ImmutableList<BlockParameter>.Empty.ToBuilder();
            foreach (var param in Parameters)
            {
                newParameters.Add(param.Map(mapping));
            }

            var newFlowInsns = new List<Instruction>();
            foreach (var insn in Flow.Instructions)
            {
                newFlowInsns.Add(insn.Map(mapping));
            }

            return new BasicBlockData(
                newParameters.ToImmutable(),
                InstructionTags,
                Flow.WithInstructions(newFlowInsns));
        }
    }
}