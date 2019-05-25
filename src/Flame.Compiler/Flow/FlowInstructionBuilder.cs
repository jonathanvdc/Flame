using System;
using System.Collections.Generic;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// An instruction reference to an unnamed instruction in
    /// block flow.
    /// </summary>
    public abstract class FlowInstructionBuilder : InstructionBuilder
    {
        /// <summary>
        /// Creates a flow instruction builder.
        /// </summary>
        /// <param name="block">The block that defines the flow.</param>
        public FlowInstructionBuilder(BasicBlockBuilder block)
        {
            this.block = block;
            this.Flow = block.Flow;
        }

        /// <summary>
        /// The block that defines the flow.
        /// </summary>
        protected BasicBlockBuilder block;

        /// <summary>
        /// Gets the block that defines the block flow.
        /// </summary>
        /// <value>A basic block builder.</value>
        public override BasicBlockBuilder Block => block;

        /// <summary>
        /// Gets the flow that defines the unnamed instruction.
        /// </summary>
        public BlockFlow Flow { get; protected set; }

        /// <inheritdoc/>
        public override bool IsValid => Flow == Block.Flow;

        /// <inheritdoc/>
        public override NamedInstructionBuilder InsertBefore(Instruction instruction, ValueTag tag)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot prepend an instruction to an invalid instruction builder.");
            }

            return block.AppendInstruction(instruction, tag);
        }
    }

    /// <summary>
    /// An instruction reference to an unnamed instruction in block
    /// flow that simply runs a single instruction and then uses
    /// its result, i.e., the instruction may be spilled into the
    /// enclosing block without changing the program's semantics.
    /// </summary>
    internal sealed class SimpleFlowInstructionBuilder : FlowInstructionBuilder
    {
        public SimpleFlowInstructionBuilder(BasicBlockBuilder block)
            : base(block)
        { }

        public override Instruction Instruction
        {
            get
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Cannot query an invalid instruction reference.");
                }

                return Flow.Instructions[0];
            }
            set
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Cannot replace an invalid instruction reference.");
                }

                Block.Flow = Flow = Flow.WithInstructions(new[] { value });
            }
        }

        public override void ReplaceInstruction(FlowGraph graph, IReadOnlyList<ValueTag> arguments)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot replace an invalid instruction reference.");
            }

            if (graph.EntryPoint.Flow is ReturnFlow)
            {
                // This is the fairly common case where an instruction is replaced by
                // a control-flow graph that consists of a single block that returns
                // immediately.
                //
                // Append the contents of implementation graph's entry point to this
                // block and set the instruction to the result value.
                var retFlow = (ReturnFlow)Block.CopyInstructionsFrom(
                    Block.InstructionTags.Count,
                    graph.EntryPoint,
                    arguments);

                Instruction = retFlow.ReturnValue;
            }
            else
            {
                // Create a continuation block to which 'return' flow can
                // branch.
                var continuationBlock = Block.Graph.AddBasicBlock();
                var resultParam = new BlockParameter(Instruction.ResultType);
                continuationBlock.AppendParameter(resultParam);
                continuationBlock.Flow = Flow.WithInstructions(
                    new[] { Instruction.CreateCopy(resultParam.Type, resultParam.Tag) });

                // Include the implementation graph into this graph.
                var entryTag = Block.Graph.Include(
                    graph,
                    (retFlow, enclosingBlock) =>
                    {
                        ValueTag resultTag = enclosingBlock.AppendInstruction(retFlow.ReturnValue);
                        return new JumpFlow(continuationBlock, new[] { resultTag });
                    });

                // Jump to the graph.
                Block.Flow = new JumpFlow(entryTag, arguments);

                // Set the block that defines this flow to the continuation block.
                block = continuationBlock;
                Flow = Block.Flow;
            }
        }
    }

    /// <summary>
    /// An instruction reference to an unnamed instruction in 'try'
    /// flow.
    /// </summary>
    internal sealed class TryFlowInstructionBuilder : FlowInstructionBuilder
    {
        public TryFlowInstructionBuilder(BasicBlockBuilder block)
            : base(block)
        { }

        public override Instruction Instruction
        {
            get
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Cannot query an invalid instruction reference.");
                }

                return Flow.Instructions[0];
            }
            set
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("Cannot replace an invalid instruction reference.");
                }

                Block.Flow = Flow = Flow.WithInstructions(new[] { value });
            }
        }

        public override void ReplaceInstruction(FlowGraph graph, IReadOnlyList<ValueTag> arguments)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot replace an invalid instruction reference.");
            }

            var tryFlow = (TryFlow)Flow;

            // Include `graph` in the defining graph. Wrap exception-throwing instructions
            // in 'try' flow.
            var entryTag = Block.Graph.Include(
                graph,
                (retFlow, enclosingBlock) =>
                {
                    // Rewrite 'return' flow by replacing it with a branch to the success
                    // block.
                    var resultTag = enclosingBlock.AppendInstruction(retFlow.ReturnValue);
                    return new JumpFlow(
                        tryFlow.SuccessBranch.MapArguments(
                            arg => arg.IsTryResult ? BranchArgument.FromValue(resultTag) : arg));
                },
                tryFlow.ExceptionBranch);

            // Jump to `graph`'s entry point.
            Block.Flow = new JumpFlow(entryTag, arguments);

            // Invalidate the instruction reference.
            Flow = null;
        }
    }
}
