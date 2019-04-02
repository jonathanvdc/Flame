using Flame.Compiler.Instructions;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// An instruction reference to an unnamed instruction in
    /// block flow.
    /// </summary>
    public abstract class FlowInstructionRef : MutableInstructionRef
    {
        public FlowInstructionRef(BasicBlockBuilder block)
        {
            this.Block = block;
        }

        /// <summary>
        /// Gets the block that defines the block flow.
        /// </summary>
        /// <value>A basic block builder.</value>
        public BasicBlockBuilder Block { get; protected set; }

        /// <summary>
        /// Gets the flow that defines the unnamed instruction.
        /// </summary>
        public BlockFlow Flow => Block.Flow;
    }

    /// <summary>
    /// An instruction reference to an unnamed instruction in block
    /// flow that simply runs a single instruction and then uses
    /// its result, i.e., the instruction may be spilled into the
    /// enclosing block without changing the program's semantics.
    /// </summary>
    internal sealed class SimpleFlowInstructionRef : FlowInstructionRef
    {
        public SimpleFlowInstructionRef(BasicBlockBuilder block)
            : base(block)
        { }

        public override Instruction Instruction
        {
            get
            {
                return Flow.Instructions[0];
            }
            set
            {
                Block.Flow = Flow.WithInstructions(new[] { value });
            }
        }

        public override void ReplaceInstruction(FlowGraph graph)
        {
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
                    Instruction.Arguments);

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
                Block.Flow = new JumpFlow(entryTag, Instruction.Arguments);

                // Set the block that defines this flow to the continuation block.
                Block = continuationBlock;
            }
        }
    }

    /// <summary>
    /// An instruction reference to an unnamed instruction in 'try'
    /// flow.
    /// </summary>
    internal sealed class TryFlowInstructionRef : FlowInstructionRef
    {
        public TryFlowInstructionRef(BasicBlockBuilder block)
            : base(block)
        { }

        public override Instruction Instruction
        {
            get
            {
                return Flow.Instructions[0];
            }
            set
            {
                Block.Flow = Flow.WithInstructions(new[] { value });
            }
        }

        public override void ReplaceInstruction(FlowGraph graph)
        {
            var tryFlow = (TryFlow)Flow;

            // Create a continuation block to which 'return' flow can branch.
            var continuationBlock = Block.Graph.AddBasicBlock();
            var resultParam = new BlockParameter(Instruction.ResultType);
            continuationBlock.AppendParameter(resultParam);
            continuationBlock.Flow = tryFlow.WithInstructions(
                new[] { Instruction.CreateCopy(resultParam.Type, resultParam.Tag) });

            var entryTag = Block.Graph.Include(
                graph,
                (retFlow, enclosingBlock) =>
                {
                    ValueTag resultTag = enclosingBlock.AppendInstruction(retFlow.ReturnValue);
                    return new JumpFlow(continuationBlock, new[] { resultTag });
                },
                tryFlow.ExceptionBranch);

            // Jump to the graph.
            Block.Flow = new JumpFlow(entryTag, Instruction.Arguments);

            // Set the block that defines this flow to the continuation block.
            Block = continuationBlock;
        }
    }
}
