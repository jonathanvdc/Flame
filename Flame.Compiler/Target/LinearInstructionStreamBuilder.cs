using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;

namespace Flame.Compiler.Target
{
    /// <summary>
    /// Translates flow graphs to linear sequences of target-specific instructions.
    /// </summary>
    public struct LinearInstructionStreamBuilder<TInstruction>
    {
        /// <summary>
        /// Creates a linear instruction stream builder.
        /// </summary>
        /// <param name="instructionSelector">
        /// The instruction selector to use.
        /// </param>
        public LinearInstructionStreamBuilder(
            ILinearInstructionSelector<TInstruction> instructionSelector)
        {
            this.InstructionSelector = instructionSelector;
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
                        selectionWorklist.Enqueue (tag);
                    }
                }
            }

            // Select target-specific instructions for reachable block flows.
            // Also order the basic blocks.
            var flowLayout = new List<BasicBlockTag>();
            var flowSelection = new Dictionary<BasicBlockTag, IReadOnlyList<TInstruction>>();
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

                var selInstructions = selection.Instructions;
                if (fallthrough != null)
                {
                    if (flowSelection.ContainsKey(fallthrough))
                    {
                        // We found a fallthrough block that has already been selected.
                        // This is quite unfortunate; we'll have to introduce a branch.
                        var insns = new List<TInstruction>(selInstructions);
                        insns.AddRange(InstructionSelector.CreateJumpTo(fallthrough));
                        selInstructions = insns;
                    }
                    else
                    {
                        // We found a fallthrough block that has not been selected yet.
                        // Add it to the flow worklist last (because the "worklist" is
                        // actually a stack) to see it get emitted right after this block.
                        flowWorklist.Push(fallthrough);
                    }
                }

                flowSelection[tag] = selInstructions;
                foreach (var item in selection.Dependencies)
                {
                    selectionWorklist.Enqueue(item);
                }
            }

            // Select target-specific instructions.
            var instructionSelection = new Dictionary<ValueTag, IReadOnlyList<TInstruction>>();
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

                instructionSelection[tag] = selection.Instructions;
                foreach (var item in selection.Dependencies)
                {
                    selectionWorklist.Enqueue(item);
                }
            }

            // We have selected target-specific instructions for reachable IR block
            // flows and required IR instructions. All we need to do now is patch them
            // together into a linear sequence of target-specific instructions.
            var instructionStream = new List<TInstruction>();
            foreach (var blockTag in flowLayout)
            {
                instructionStream.AddRange(InstructionSelector.CreateBlockMarker(graph.GetBasicBlock(blockTag)));
                foreach (var insnTag in graph.GetBasicBlock(blockTag).InstructionTags)
                {
                    IReadOnlyList<TInstruction> selection;
                    if (instructionSelection.TryGetValue(insnTag, out selection))
                    {
                        instructionStream.AddRange(selection);
                    }
                }
                instructionStream.AddRange(flowSelection[blockTag]);
            }
            return instructionStream;
        }
    }
}
