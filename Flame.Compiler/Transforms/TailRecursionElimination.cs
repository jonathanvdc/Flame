using System.Collections.Generic;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transform that rewrites static calls to the current method
    /// just prior to a return as unconditional jumps to the entry point.
    /// </summary>
    public sealed class TailRecursionElimination : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates a tail recursion elimination transform for a particular method.
        /// </summary>
        /// <param name="method">The method to optimize.</param>
        public TailRecursionElimination(IMethod method)
        {
            this.Method = method;
        }

        /// <summary>
        /// Gets the method being optimized.
        /// </summary>
        /// <value>The method being optimized.</value>
        public IMethod Method { get; private set; }

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var builder = graph.ToBuilder();
            var ordering = graph.GetAnalysisResult<InstructionOrdering>();
            foreach (var block in builder.BasicBlocks)
            {
                if (block.Flow is ReturnFlow)
                {
                    var retFlow = (ReturnFlow)block.Flow;
                    TryReplaceWithBranchToEntry(
                        block,
                        retFlow.ReturnValue,
                        builder,
                        ordering);
                }
            }
            return builder.ToImmutable();
        }

        private void TryReplaceWithBranchToEntry(
            BasicBlockBuilder block,
            Instruction instruction,
            FlowGraphBuilder graph,
            InstructionOrdering ordering)
        {
            var insnsToEliminate = new HashSet<ValueTag>();
            ValueTag callTag = null;
            while (instruction.Prototype is CopyPrototype)
            {
                callTag = ((CopyPrototype)instruction.Prototype).GetCopiedValue(instruction);
                insnsToEliminate.Add(callTag);
                if (!graph.ContainsInstruction(callTag))
                {
                    // We encountered a basic block parameter, which we can't really
                    // peek through.
                    return;
                }
                instruction = graph.GetInstruction(callTag).Instruction;
            }

            if ((callTag == null || graph.GetValueParent(callTag).Tag == block.Tag)
                && IsSelfCallPrototype(instruction.Prototype))
            {
                // Now all we have to do is make sure that there is no instruction
                // that must run after the call.
                if (callTag != null)
                {
                    var selection = graph.GetInstruction(callTag).NextInstructionOrNull;
                    while (selection != null)
                    {
                        if (ordering.MustRunBefore(callTag, selection))
                        {
                            // Aw, snap. Looks like we can't reorder the call, which
                            // means no tail call elimination. Abort.
                            return;
                        }
                        selection = selection.NextInstructionOrNull;
                    }
                }

                // Remove some instructions.
                foreach (var tag in insnsToEliminate)
                {
                    graph.RemoveInstruction(tag);
                }

                // Turn the return flow into a jump.
                block.Flow = new JumpFlow(graph.EntryPointTag, instruction.Arguments);
            }
        }

        private bool IsSelfCallPrototype(InstructionPrototype prototype)
        {
            if (prototype is CallPrototype)
            {
                var callProto = (CallPrototype)prototype;
                return callProto.Lookup == MethodLookup.Static
                    && callProto.Callee.Equals(Method);
            }
            else
            {
                return false;
            }
        }
    }
}
