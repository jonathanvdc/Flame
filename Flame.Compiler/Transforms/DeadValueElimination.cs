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

            // Delete everything that's not live.
            var graphBuilder = graph.ToBuilder();
            foreach (var block in graphBuilder.BasicBlocks)
            {
                // Remove dead basic block parameters.
                block.Parameters = ImmutableList.CreateRange(
                    block.Parameters.Where(param => liveValues.Contains(param.Tag)));

                // Remove arguments to deleted parameters.
                block.Flow = block.Flow.WithBranches(
                    block.Flow.Branches.Select(
                        branch =>
                        {
                            var newArgs = new List<BranchArgument>();
                            foreach (var pair in ZipArgumentsAndParameters(branch, graphBuilder.ImmutableGraph))
                            {
                                if (!pair.Value.IsValue || liveValues.Contains(pair.Key))
                                {
                                    newArgs.Add(pair.Value);
                                }
                            }
                            return branch.WithArguments(newArgs);
                        }).ToArray());
            }

            // Remove all dead instructions.
            foreach (var tag in graphBuilder.InstructionTags)
            {
                if (!liveValues.Contains(tag))
                {
                    graphBuilder.RemoveInstruction(tag);
                }
            }

            return graphBuilder.ToImmutable();
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
                    foreach (var pair in ZipArgumentsAndParameters(branch, graph))
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

        /// <summary>
        /// Creates a zipped sequence of branch arguments and the
        /// basic block parameters they correspond to.
        /// </summary>
        /// <param name="branch">
        /// A branch.
        /// </param>
        /// <param name="graph">
        /// The flow graph that defines the branch.
        /// </param>
        /// <returns>
        /// A zipped sequence of basic block parameter tags and
        /// branch arguments.
        /// </returns>
        public static IEnumerable<KeyValuePair<ValueTag, BranchArgument>> ZipArgumentsAndParameters(
            Branch branch,
            FlowGraph graph)
        {
            var targetParams = graph.GetBasicBlock(branch.Target).ParameterTags;
            return targetParams.Zip(
                branch.Arguments,
                (x, y) => new KeyValuePair<ValueTag, BranchArgument>(x, y));
        }
    }
}
