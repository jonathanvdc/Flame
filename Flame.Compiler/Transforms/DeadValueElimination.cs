using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Compiler.Analysis;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// Removes unused, non-effectful instructions and basic block
    /// parameters from flow graphs.
    /// </summary>
    public static class DeadValueElimination
    {
        /// <summary>
        /// Removes unused, non-effectful instructions and basic block
        /// parameters from a flow graph.
        /// </summary>
        /// <param name="graph">The flow graph to transform.</param>
        /// <returns>A transformed flow graph.</returns>
        public static FlowGraph Apply(FlowGraph graph)
        {
            // This transform simply builds a set of live values and
            // then delete everything that's not.

            // Compute the live values.
            var liveValues = ComputeLiveValues(graph);

            // Compute the set of dead values.
            var deadValues = new HashSet<ValueTag>(graph.ValueTags);
            deadValues.ExceptWith(liveValues);

            // Delete everything that's not live.
            return graph.RemoveDefinitions(deadValues);
        }

        /// <summary>
        /// Computes the set of all live values in the graph.
        /// </summary>
        /// <param name="graph">The graph to inspect.</param>
        /// <returns>A set of live values.</returns>
        private static HashSet<ValueTag> ComputeLiveValues(FlowGraph graph)
        {
            var liveValues = new HashSet<ValueTag>();

            // Effectful instructions are always live.
            liveValues.UnionWith(graph.GetAnalysisResult<EffectfulInstructions>().Instructions);

            // Entry point parameters are always live too.
            liveValues.UnionWith(graph.GetBasicBlock(graph.EntryPointTag).ParameterTags);

            // Also construct a mapping of basic block parameters to the
            // arguments they take.
            var phiArgs = new Dictionary<ValueTag, HashSet<ValueTag>>();
            foreach (var param in graph.ParameterTags)
            {
                phiArgs[param] = new HashSet<ValueTag>();
            }

            // Instructions that are part of block flows are live.
            foreach (var block in graph.BasicBlocks)
            {
                var flow = block.Flow;
                foreach (var instruction in flow.Instructions)
                {
                    liveValues.UnionWith(instruction.Arguments);
                }

                // While we're at it, we might as well populate `phiArgs`.
                foreach (var branch in flow.Branches)
                {
                    foreach (var pair in branch.ZipArgumentsWithParameters(graph))
                    {
                        if (pair.Value.IsValue)
                        {
                            phiArgs[pair.Key].Add(pair.Value.ValueOrNull);
                        }
                    }
                }
            }

            // Now we simply compute the transitive closure of
            // the set of live values.
            var worklist = new Queue<ValueTag>(liveValues);
            liveValues.Clear();
            while (worklist.Count > 0)
            {
                var value = worklist.Dequeue();
                if (liveValues.Add(value))
                {
                    HashSet<ValueTag> args;
                    if (phiArgs.TryGetValue(value, out args))
                    {
                        foreach (var arg in args)
                        {
                            worklist.Enqueue(arg);
                        }
                    }
                    else
                    {
                        foreach (var arg in graph.GetInstruction(value).Instruction.Arguments)
                        {
                            worklist.Enqueue(arg);
                        }
                    }
                }
            }

            return liveValues;
        }
    }
}
