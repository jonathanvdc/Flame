using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;

namespace Flame.Compiler.Target
{
    /// <summary>
    /// Translates flow graphs to linear sequences of target-specific instructions.
    /// </summary>
    /// <typeparam name="TInstruction">The type of instruction to generate.</typeparam>
    /// <remarks>
    /// <c>InstructionStreamBuilder</c> assumes that the instruction selector handles
    /// data transfer between instructions. This is a reasonable abstraction for
    /// register-based (virtual) machines, but may not be a good fit for stack machines.
    ///
    /// See <see cref="StackInstructionStreamBuilder{TInstruction}"/> for an instruction
    /// stream builder that manages data transfer using a combination of stack slots and
    /// explicit register loads/stores.
    /// </remarks>
    public class InstructionStreamBuilder<TInstruction>
    {
        /// <summary>
        /// Creates a linear instruction stream builder.
        /// </summary>
        /// <param name="instructionSelector">
        /// The instruction selector to use.
        /// </param>
        protected InstructionStreamBuilder(
            ILinearInstructionSelector<TInstruction> instructionSelector)
        {
            this.InstructionSelector = instructionSelector;
        }

        /// <summary>
        /// Creates a linear instruction stream builder.
        /// </summary>
        /// <param name="instructionSelector">
        /// The instruction selector to use.
        /// </param>
        public static InstructionStreamBuilder<TInstruction> Create<TSelector>(
            TSelector instructionSelector)
            where TSelector : ILinearInstructionSelector<TInstruction>
        {
            return new InstructionStreamBuilder<TInstruction>(instructionSelector);
        }

        /// <summary>
        /// Gets the instruction selector used by this linear
        /// instruction stream builder.
        /// </summary>
        /// <value>
        /// A linear instruction selector.
        /// </value>
        public ILinearInstructionSelector<TInstruction> InstructionSelector { get; private set; }

        /// <summary>
        /// Takes a flow graph and translates it to an instruction stream.
        /// </summary>
        /// <param name="graph">
        /// The flow graph to translate.
        /// </param>
        /// <returns>
        /// A linear sequence of target-specific instructions.
        /// </returns>
        public IReadOnlyList<TInstruction> ToInstructionStream(FlowGraph graph)
        {
            // One thing to keep in mind when selecting instructions is that
            // not all IR instructions must be translated to target-specific instructions.
            //
            // For example, suppose that the target has a specialized add-constant-integer
            // instruction---let's call it `addi`. Then the following sequence of instructions
            //
            //     one = const(1, System::Int32)();
            //     addition = intrinsic(@arith.add, System::Int32, #(System::Int32, System::Int32))(arg, one);
            //
            // should get translated to
            //
            //     <addition> = addi <arg>, 1
            //
            // Note how the `one` instruction doesn't get emitted despite the fact that
            // is it *not* dead from an IR point of view. That's definitely a good thing
            // and it's not something we'll get if we naively create a linear stream of
            // instructions by simply selecting instructions for each IR instruction.
            //
            // To ensure that we select only the instructions we actually need, we will
            // start at
            //
            //   1. "root" instructions: reachable instructions that may have side-effects
            //       and hence *must* be selected, and
            //
            //   2. block flows.
            //
            // Furthermore, we also want to make sure that we emit only reachable basic blocks.
            // All other basic blocks are dead code and we shouldn't bother selecting instructions
            // for them.
            //
            // To figure out which instructions are effectful instructions and which blocks are
            // reachable, we will rely on a couple of analyses.

            var reachability = graph.GetAnalysisResult<BlockReachability>();
            var effectful = graph.GetAnalysisResult<EffectfulInstructions>();

            // Find the exact set of root instructions. Add them all
            // to a queue of instructions to select.
            var selectionWorklist = new Queue<ValueTag>();
            foreach (var block in graph.BasicBlocks)
            {
                if (!reachability.IsReachableFrom(graph.EntryPointTag, block))
                {
                    continue;
                }

                foreach (var tag in block.InstructionTags.Reverse())
                {
                    if (effectful.Instructions.Contains(tag))
                    {
                        selectionWorklist.Enqueue(tag);
                    }
                }
            }

            // Select target-specific instructions for reachable block flows.
            // Also order the basic blocks.
            var flowLayout = new List<BasicBlockTag>();
            var flowSelection = new Dictionary<BasicBlockTag, SelectedInstructions<TInstruction>>();
            var flowWorklist = new Stack<BasicBlockTag>();
            flowWorklist.Push(graph.EntryPointTag);
            while (flowWorklist.Count > 0)
            {
                var tag = flowWorklist.Pop();
                if (flowSelection.ContainsKey(tag))
                {
                    // Never select blocks twice.
                    continue;
                }

                // Assign a dummy value (null) to the block tag so we don't fool
                // ourselves into thinking that the block isn't being processed
                // yet.
                flowSelection[tag] = default(SelectedInstructions<TInstruction>);

                // Add the block to the flow layout.
                flowLayout.Add(tag);

                // Fetch the block's flow from the graph.
                var block = graph.GetBasicBlock(tag);
                var flow = block.Flow;

                // Select instructions for the flow.
                BasicBlockTag fallthrough;
                var selection = InstructionSelector.SelectInstructions(
                    flow,
                    block.Tag,
                    graph,
                    flow.BranchTargets.FirstOrDefault(target => !flowSelection.ContainsKey(target)),
                    out fallthrough);

                // Emit all branch targets.
                foreach (var target in flow.BranchTargets)
                {
                    flowWorklist.Push(target);
                }

                if (fallthrough != null)
                {
                    if (flowSelection.ContainsKey(fallthrough))
                    {
                        // We found a fallthrough block that has already been selected.
                        // This is quite unfortunate; we'll have to introduce a branch.
                        selection = selection.Append(InstructionSelector.CreateJumpTo(fallthrough));
                    }
                    else
                    {
                        // We found a fallthrough block that has not been selected yet.
                        // Add it to the flow worklist last (because the "worklist" is
                        // actually a stack) to see it get emitted right after this block.
                        flowWorklist.Push(fallthrough);
                    }
                }

                flowSelection[tag] = selection;
                foreach (var item in selection.Dependencies)
                {
                    selectionWorklist.Enqueue(item);
                }
            }

            // Select target-specific instructions.
            var instructionSelection = new Dictionary<ValueTag, SelectedInstructions<TInstruction>>();
            while (selectionWorklist.Count > 0)
            {
                var tag = selectionWorklist.Dequeue();
                if (instructionSelection.ContainsKey(tag)
                    || graph.ContainsBlockParameter(tag))
                {
                    // Never select instructions twice. Also, don't try
                    // to "select" block parameters.
                    continue;
                }

                var instruction = graph.GetInstruction(tag);
                var selection = InstructionSelector.SelectInstructions(instruction);

                instructionSelection[tag] = selection;
                foreach (var item in selection.Dependencies)
                {
                    selectionWorklist.Enqueue(item);
                }
            }

            // We have selected target-specific instructions for reachable IR block
            // flows and required IR instructions. All we need to do now is patch them
            // together into a linear sequence of target-specific instructions.
            return ToInstructionStream(
                flowLayout.Select(graph.GetBasicBlock),
                instructionSelection,
                flowSelection);
        }

        /// <summary>
        /// Creates a linear sequence of instructions for a control-flow graph based
        /// on an order to place basic blocks in and selected instructions for named
        /// instructions and block flow.
        /// </summary>
        /// <param name="layout">
        /// The basic blocks to place, in the order they must be placed.
        /// </param>
        /// <param name="instructions">
        /// A mapping of named instructions to their selected instructions. Named
        /// instructions that do not appear in this mapping should not be selected.
        /// </param>
        /// <param name="flow">
        /// A mapping of basic block tags to the selected instructions for their control
        /// flow.
        /// </param>
        /// <returns>
        /// A linear sequence of instructions.
        /// </returns>
        private IReadOnlyList<TInstruction> ToInstructionStream(
            IEnumerable<BasicBlock> layout,
            IReadOnlyDictionary<ValueTag, SelectedInstructions<TInstruction>> instructions,
            IReadOnlyDictionary<BasicBlockTag, SelectedInstructions<TInstruction>> flow)
        {
            var instructionStream = new List<TInstruction>();
            foreach (var block in layout)
            {
                instructionStream.AddRange(InstructionSelector.CreateBlockMarker(block));
                instructionStream.AddRange(ToInstructionStream(block, instructions, flow[block]));
            }
            return instructionStream;
        }

        /// <summary>
        /// Creates a linear sequence of instructions for a basic block based on
        /// selected instructions for named instructions and block flow.
        /// </summary>
        /// <param name="block">
        /// The basic blocks to place.
        /// </param>
        /// <param name="instructions">
        /// A mapping of named instructions to their selected instructions. Named
        /// instructions that do not appear in this mapping should not be selected.
        /// </param>
        /// <param name="flow">
        /// Selected instructions for <paramref name="block"/>'s control flow.
        /// </param>
        /// <returns>
        /// A linear sequence of instructions.
        /// </returns>
        protected virtual IReadOnlyList<TInstruction> ToInstructionStream(
            BasicBlock block,
            IReadOnlyDictionary<ValueTag, SelectedInstructions<TInstruction>> instructions,
            SelectedInstructions<TInstruction> flow)
        {
            var instructionStream = new List<TInstruction>();
            foreach (var insnTag in block.InstructionTags)
            {
                SelectedInstructions<TInstruction> selection;
                if (instructions.TryGetValue(insnTag, out selection))
                {
                    instructionStream.AddRange(selection.Instructions);
                }
            }
            instructionStream.AddRange(flow.Instructions);
            return instructionStream;
        }
    }

    /// <summary>
    /// An instruction stream builder that manages data transfer using a combination of
    /// stack slots and explicit register loads/stores, as commonly offered by stack
    /// machines.
    /// </summary>
    /// <typeparam name="TInstruction">The type of instruction to generate.</typeparam>
    public class StackInstructionStreamBuilder<TInstruction> : InstructionStreamBuilder<TInstruction>
    {
        /// <summary>
        /// Creates a stack machine instruction stream builder.
        /// </summary>
        /// <param name="instructionSelector">
        /// The instruction selector to use. This instruction selector must
        /// also be a stack instruction selector.
        /// </param>
        protected StackInstructionStreamBuilder(
            ILinearInstructionSelector<TInstruction> instructionSelector)
            : base(instructionSelector)
        { }

        /// <summary>
        /// Creates a linear instruction stream builder.
        /// </summary>
        /// <param name="instructionSelector">
        /// The instruction selector to use.
        /// </param>
        public static new StackInstructionStreamBuilder<TInstruction> Create<TSelector>(
            TSelector instructionSelector)
            where TSelector : ILinearInstructionSelector<TInstruction>, IStackInstructionSelector<TInstruction>
        {
            return new StackInstructionStreamBuilder<TInstruction>(instructionSelector);
        }

        private IStackInstructionSelector<TInstruction> StackSelector =>
            (IStackInstructionSelector<TInstruction>)InstructionSelector;

        /// <summary>
        /// Gets the contents of the evaluation stack just before a basic block's
        /// first instruction is executed.
        /// </summary>
        /// <param name="block">The basic block to inspect.</param>
        /// <returns>A sequence of values that represent the contents of the stack.</returns>
        protected virtual IEnumerable<ValueTag> GetStackContentsOnEntry(BasicBlock block)
        {
            return Enumerable.Empty<ValueTag>();
        }

        /// <inheritdoc/>
        protected override IReadOnlyList<TInstruction> ToInstructionStream(
            BasicBlock block,
            IReadOnlyDictionary<ValueTag, SelectedInstructions<TInstruction>> instructions,
            SelectedInstructions<TInstruction> flow)
        {
            var uses = block.Graph.GetAnalysisResult<ValueUses>();

            // We will maintain an evaluation stack as we generate instructions
            // for the basic block.
            var evalStack = new Stack<ValueTag>(GetStackContentsOnEntry(block));

            // We will maintain a linked list of sequences of selected instructions.
            // The idea behind this data structure is that we'll be able to insert
            // instructions later.
            var instructionBlobs = new LinkedList<IReadOnlyList<TInstruction>>();

            // Create a mapping of values to linked list nodes after which we may
            // spill those values. These "spill points" are the earliest opportunity
            // at which these values may be spilled or popped.
            var spillPoints = new Dictionary<ValueTag, LinkedListNode<IReadOnlyList<TInstruction>>>();

            foreach (var insnTag in block.InstructionTags)
            {
                SelectedInstructions<TInstruction> selection;
                if (instructions.TryGetValue(insnTag, out selection))
                {
                    // Line up arguments.
                    PrepareArguments(selection.Dependencies, evalStack, instructionBlobs, spillPoints, block.Graph);

                    // Emit the instruction's implementation.
                    instructionBlobs.AddLast(selection.Instructions);

                    // Push the result on the stack, if there is a result.
                    var insn = block.Graph.GetInstruction(insnTag);
                    if (StackSelector.Pushes(insn.Prototype))
                    {
                        if (uses.IsUsedOutsideOf(insnTag, block))
                        {
                            // We need to spill this value.
                            instructionBlobs.AddLast(
                                StackSelector.CreateStoreRegister(
                                    insnTag,
                                    insn.ResultType));
                        }
                        else
                        {
                            // Otherwise, we'll put it on the stack. We also want to set up a
                            // spill point so this value can be spilled efficiently if need be.
                            evalStack.Push(insnTag);
                            spillPoints[insnTag] = instructionBlobs.Last;
                        }
                    }
                }
            }

            // Prepare arguments for the block's outgoing flow.
            PrepareArguments(flow.Dependencies, evalStack, instructionBlobs, spillPoints, block.Graph);

            // Spill everything else that's still on the stack.
            while (evalStack.Count > 0)
            {
                var stackValue = evalStack.Pop();
                instructionBlobs.AddAfter(
                    spillPoints[stackValue],
                    StackSelector.CreateStoreRegister(
                        stackValue,
                        block.Graph.GetValueType(stackValue)));
            }

            // Append the block's outgoing flow instructions.
            instructionBlobs.AddLast(flow.Instructions);
            return instructionBlobs.SelectMany(list => list).ToArray();
        }

        private void PrepareArguments(
            IReadOnlyList<ValueTag> dependencies,
            Stack<ValueTag> evalStack,
            LinkedList<IReadOnlyList<TInstruction>> instructionBlobs,
            IReadOnlyDictionary<ValueTag, LinkedListNode<IReadOnlyList<TInstruction>>> spillPoints,
            FlowGraph graph)
        {
            foreach (var dependency in dependencies.Reverse())
            {
                if (evalStack.Contains(dependency))
                {
                    if (evalStack.Peek() == dependency)
                    {
                        // We already have this argument right where we want it.
                        evalStack.Pop();
                        continue;
                    }
                    else
                    {
                        // The argument is on the stack but not in the place where
                        // we want it. We can do one of two things.
                        //   1. Spill stack values until the value appears right where
                        //      we want it.
                        //   2. Spill the value itself, leaving the rest of the stack
                        //      alone. We then load the value.
                        //
                        // For now, we'll just always go with option two.

                        // Spill the value.
                        instructionBlobs.AddAfter(
                            spillPoints[dependency],
                            StackSelector.CreateStoreRegister(
                                dependency,
                                graph.GetValueType(dependency)));

                        // Remove it from the stack.
                        var tempStack = new Stack<ValueTag>();
                        while (evalStack.Peek() != dependency)
                        {
                            tempStack.Push(evalStack.Pop());
                        }
                        evalStack.Pop();
                        while (tempStack.Count > 0)
                        {
                            evalStack.Push(tempStack.Pop());
                        }
                    }
                }

                // Argument is not on the stack. Load it from its register.
                instructionBlobs.AddLast(
                    StackSelector.CreateLoadRegister(
                        dependency,
                        graph.GetValueType(dependency)));
            }
        }
    }
}
