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

            var builder = graph.ToBuilder();
            foreach (var instruction in builder.Instructions)
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
                        instruction.Instruction = Instruction.CreateCopy(
                            instruction.ResultType,
                            storeProto.GetValue(instruction.Instruction));
                    }
                }
            }

            return builder.ToImmutable();
        }
    }
}
