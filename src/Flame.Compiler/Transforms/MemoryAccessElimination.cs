using System.Collections.Generic;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A pass that tries to eliminate loads and stores, replacing them with
    /// local value copies instead.
    /// </summary>
    public sealed class MemoryAccessElimination : IntraproceduralOptimization
    {
        private MemoryAccessElimination()
        { }

        /// <summary>
        /// An instance of the memory access elimination pass.
        /// </summary>
        public static readonly MemoryAccessElimination Instance
            = new MemoryAccessElimination();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var memSSA = graph.GetAnalysisResult<MemorySSA>();
            var aliasing = graph.GetAnalysisResult<AliasAnalysisResult>();
            var effectfulness = graph.GetAnalysisResult<EffectfulInstructions>();

            var builder = graph.ToBuilder();

            // First try and eliminate loads and stores based on their memory SSA
            // states.
            foreach (var instruction in builder.NamedInstructions)
            {
                var proto = instruction.Prototype;
                if (proto is LoadPrototype)
                {
                    // Loads can be eliminated if we know the memory contents.
                    var loadProto = (LoadPrototype)proto;
                    var state = memSSA.GetMemoryAfter(instruction);
                    ValueTag value;
                    if (state.TryGetValueAt(
                        loadProto.GetPointer(instruction.Instruction),
                        graph,
                        out value))
                    {
                        instruction.Instruction = Instruction.CreateCopy(
                            instruction.ResultType,
                            value);
                    }
                }
                else if (proto is StorePrototype)
                {
                    // Stores can be eliminated if they don't affect the memory state.
                    var storeProto = (StorePrototype)proto;
                    var stateBefore = memSSA.GetMemoryBefore(instruction);
                    var stateAfter = memSSA.GetMemoryAfter(instruction);
                    if (stateBefore == stateAfter)
                    {
                        EliminateStore(instruction);
                    }
                }
            }

            // Then try to coalesce stores by iterating through basic blocks.
            foreach (var block in builder.BasicBlocks)
            {
                var pendingStores = new List<NamedInstructionBuilder>();
                foreach (var instruction in block.NamedInstructions)
                {
                    var proto = instruction.Prototype;
                    if (proto is StorePrototype)
                    {
                        var storeProto = (StorePrototype)proto;
                        var pointer = storeProto.GetPointer(instruction.Instruction);

                        var newPending = new List<NamedInstructionBuilder>();
                        foreach (var pending in pendingStores)
                        {
                            var pendingProto = (StorePrototype)pending.Prototype;
                            var pendingPointer = pendingProto.GetPointer(pending.Instruction);
                            var aliasState = aliasing.GetAliasing(pointer, pendingPointer);
                            if (aliasState == Aliasing.MustAlias)
                            {
                                // Yes, do it. Delete the pending store.
                                EliminateStore(pending);
                            }
                            else if (aliasState == Aliasing.NoAlias)
                            {
                                // We can't eliminate the pending store, but we can keep it
                                // in the pending list.
                                newPending.Add(pending);
                            }
                        }
                        pendingStores = newPending;

                        // Add this store to the list of pending stores as well.
                        pendingStores.Add(instruction);
                    }
                    else if (proto is LoadPrototype || proto is CopyPrototype)
                    {
                        // Loads are perfectly benign. They *may* be effectful in the sense
                        // that they can trigger a segfault and make the program blow up, but
                        // there's no way we can catch that anyway. We'll just allow store
                        // coalescing across load boundaries.
                        //
                        // We also want to test for copy instructions here because our effectfulness
                        // analysis is slightly outdated and we may have turned loads/stores into copies.
                    }
                    else if (effectfulness.Instructions.Contains(instruction))
                    {
                        // Effectful instructions are a barrier for store coalescing.
                        pendingStores.Clear();
                    }
                }
            }

            return builder.ToImmutable();
        }

        private void EliminateStore(NamedInstructionBuilder instruction)
        {
            var storeProto = (StorePrototype)instruction.Prototype;
            instruction.Instruction = Instruction.CreateCopy(
                instruction.ResultType,
                storeProto.GetValue(instruction.Instruction));
        }
    }
}
