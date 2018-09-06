using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A mapping that describes where values are used.
    /// </summary>
    public struct ValueUses
    {
        internal ValueUses(
            ImmutableDictionary<ValueTag, ImmutableHashSet<ValueTag>> instructions,
            ImmutableDictionary<ValueTag, ImmutableHashSet<BasicBlockTag>> flow)
        {
            this.instructions = instructions;
            this.flow = flow;
        }

        internal ImmutableDictionary<ValueTag, ImmutableHashSet<ValueTag>> instructions;
        internal ImmutableDictionary<ValueTag, ImmutableHashSet<BasicBlockTag>> flow;

        /// <summary>
        /// Gets the set of all values that are defined by instructions
        /// that take <paramref name="tag"/> as an argument.
        /// </summary>
        /// <param name="tag">The tag to examine.</param>
        /// <returns>
        /// A set of all value tags of instructions that use <paramref name="tag"/>.
        /// </returns>
        public ImmutableHashSet<ValueTag> GetInstructionUses(ValueTag tag)
        {
            return instructions[tag];
        }

        /// <summary>
        /// Gets the set of basic block tags for all basic blocks containing flows
        /// that use <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The tag to examine.</param>
        /// <returns>
        /// A set of basic block tags for all basic blocks containing flows
        /// that use <paramref name="tag"/>.
        /// </returns>
        public ImmutableHashSet<BasicBlockTag> GetFlowUses(ValueTag tag)
        {
            return flow[tag];
        }
    }

    /// <summary>
    /// An analysis that figures out where values are used.
    /// </summary>
    public sealed class ValueUseAnalysis : IFlowGraphAnalysis<ValueUses>
    {
        private ValueUseAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the value use analysis.
        /// </summary>
        /// <returns>An instance of the value use analysis.</returns>
        public static readonly ValueUseAnalysis Instance = new ValueUseAnalysis();

        /// <inheritdoc/>
        public ValueUses Analyze(FlowGraph graph)
        {
            // Set up data structures.
            var instructions = ImmutableDictionary.CreateBuilder<ValueTag, ImmutableHashSet<ValueTag>>();
            var flow = ImmutableDictionary.CreateBuilder<ValueTag, ImmutableHashSet<BasicBlockTag>>();
            foreach (var tag in graph.InstructionTags)
            {
                instructions.Add(tag, ImmutableHashSet.Create<ValueTag>());
                flow.Add(tag, ImmutableHashSet.Create<BasicBlockTag>());
            }
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var parameter in block.Parameters)
                {
                    instructions.Add(parameter.Tag, ImmutableHashSet.Create<ValueTag>());
                    flow.Add(parameter.Tag, ImmutableHashSet.Create<BasicBlockTag>());
                }
            }

            // Analyze instructions.
            foreach (var selection in graph.Instructions)
            {
                foreach (var arg in selection.Instruction.Arguments)
                {
                    instructions[arg] = instructions[arg].Add(selection.Tag);
                }
            }

            // Analyze flows.
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var insn in block.Flow.Instructions)
                {
                    foreach (var arg in insn.Arguments)
                    {
                        flow[arg] = flow[arg].Add(block.Tag);
                    }
                }
                foreach (var branch in block.Flow.Branches)
                {
                    foreach (var arg in branch.Arguments)
                    {
                        if (arg.IsValue)
                        {
                            flow[arg.ValueOrNull] = flow[arg.ValueOrNull].Add(block.Tag);
                        }
                    }
                }
            }
            return new ValueUses(instructions.ToImmutable(), flow.ToImmutable());
        }

        /// <inheritdoc/>
        public ValueUses AnalyzeWithUpdates(
            FlowGraph graph,
            ValueUses previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            // Just re-analyze the flow graph.
            return Analyze(graph);
        }
    }
}
