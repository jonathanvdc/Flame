using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transformation that reorders instructions. It is intended to be used prior
    /// to codegen. Instruction reordering helps reduce register pressure and promotes
    /// register-to-stack conversion on virtual machines that use an evaluation stack.
    /// </summary>
    public class InstructionReordering : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates an instruction reordering pass.
        /// </summary>
        protected InstructionReordering()
        { }

        /// <summary>
        /// An instance of the instruction reordering transform.
        /// </summary>
        /// <value>An instruction reordering transform.</value>
        public static readonly InstructionReordering Instance
            = new InstructionReordering();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var ordering = graph.GetAnalysisResult<InstructionOrdering>();

            var builder = graph.ToBuilder();
            foreach (var block in builder.BasicBlocks)
            {
                // Create a linked list that contains all instructions.
                var schedule = new LinkedList<ValueTag>(block.InstructionTags);

                // Plus a dummy node for the block's flow.
                schedule.AddLast((ValueTag)null);

                // Reorder branch arguments.
                foreach (var branch in block.Flow.Branches.Reverse())
                {
                    ReorderArguments(
                        branch.Arguments
                            .Select(arg => arg.ValueOrNull)
                            .Where(arg => arg != null),
                        block,
                        schedule.Last,
                        ordering,
                        builder.ImmutableGraph);
                }

                // Reorder anonymous instruction arguments.
                foreach (var instruction in block.Flow.Instructions.Reverse())
                {
                    ReorderArguments(instruction, block, schedule.Last, ordering, builder.ImmutableGraph);
                }

                // Reorder named instruction arguments in reverse.
                var finger = schedule.Last.Previous;
                while (finger != null)
                {
                    var instruction = graph.GetInstruction(finger.Value);
                    ReorderArguments(instruction.Instruction, block, finger, ordering, builder.ImmutableGraph);
                    finger = finger.Previous;
                }

                // Materialize the new schedule.
                int index = 0;
                foreach (var value in schedule)
                {
                    if (value == null)
                    {
                        continue;
                    }

                    var instruction = builder.GetInstruction(value);
                    instruction.MoveTo(index, block);
                    index++;
                }
            }
            return builder.ToImmutable();
        }

        /// <summary>
        /// Gets an instruction's arguments and orders them by use.
        /// </summary>
        /// <param name="instruction">The instruction to inspect.</param>
        /// <returns>An ordered list of arguments.</returns>
        protected virtual IReadOnlyList<ValueTag> GetOrderedArguments(
            Instruction instruction)
        {
            return instruction.Arguments;
        }

        private void ReorderArguments(
            Instruction instruction,
            BasicBlockTag block,
            LinkedListNode<ValueTag> insertionPoint,
            InstructionOrdering ordering,
            FlowGraph graph)
        {
            ReorderArguments(
                GetOrderedArguments(instruction),
                block,
                insertionPoint,
                ordering,
                graph);
        }

        private static void ReorderArguments(
            IEnumerable<ValueTag> arguments,
            BasicBlockTag block,
            LinkedListNode<ValueTag> insertionPoint,
            InstructionOrdering ordering,
            FlowGraph graph)
        {
            foreach (var arg in arguments.Reverse())
            {
                TryReorder(arg, block, ref insertionPoint, graph);
            }
        }

        /// <summary>
        /// Tries to move an instruction from its
        /// current location to just before another instruction.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to try and move.
        /// </param>
        /// <param name="block">
        /// The block that defines the insertion point.
        /// </param>
        /// <param name="insertionPoint">
        /// An instruction to which the instruction should
        /// be moved. It must be defined in the same basic
        /// block as <paramref name="instruction"/>.
        /// </param>
        /// <param name="graph">
        /// The graph that defines both instructions.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> was
        /// successfully reordered; otherwise, <c>false</c>.
        /// </returns>
        private static bool TryReorder(
            ValueTag instruction,
            BasicBlockTag block,
            ref LinkedListNode<ValueTag> insertionPoint,
            FlowGraph graph)
        {
            if (graph.GetValueParent(instruction).Tag != block
                || !graph.ContainsInstruction(instruction))
            {
                return false;
            }

            // Grab the ordering to which we should adhere.
            var ordering = graph.GetAnalysisResult<InstructionOrdering>();

            // Start at the linked list node belonging to the instruction
            // to move and work our way toward the insertion point.
            // Check the must-run-before relation as we traverse the list.
            var instructionNode = GetInstructionNode(instruction, insertionPoint);
            var currentNode = instructionNode;
            while (currentNode != insertionPoint)
            {
                if (ordering.MustRunBefore(instruction, currentNode.Value))
                {
                    // Aw snap, we encountered a dependency.
                    // Time to abandon ship.
                    return false;
                }

                currentNode = currentNode.Next;
            }

            // Looks like we can reorder the instruction!
            if (insertionPoint != instructionNode)
            {
                insertionPoint.List.Remove(instructionNode);
                insertionPoint.List.AddBefore(insertionPoint, instructionNode);
            }
            insertionPoint = instructionNode;
            return true;
        }

        private static LinkedListNode<ValueTag> GetInstructionNode(
            ValueTag instruction,
            LinkedListNode<ValueTag> insertionPoint)
        {
            return insertionPoint.List.Find(instruction);
        }
    }
}
