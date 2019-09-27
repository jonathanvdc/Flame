using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// Removes unused, non-effectful instructions and basic block
    /// parameters from flow graphs.
    /// </summary>
    public sealed class DeadValueElimination : IntraproceduralOptimization
    {
        private DeadValueElimination()
        { }

        /// <summary>
        /// An instance of the dead value elimination transform.
        /// </summary>
        public static readonly DeadValueElimination Instance = new DeadValueElimination();

        /// <summary>
        /// Removes unused, non-effectful instructions and basic block
        /// parameters from a flow graph.
        /// </summary>
        /// <param name="graph">The flow graph to transform.</param>
        /// <returns>A transformed flow graph.</returns>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // This transform simply builds a set of live values and
            // then deletes everything that's not.

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

            // Remove stores to local variables from the set of
            // effectful instructions. Our reason for doing this is
            // that stores to dead local variables are effectively
            // dead themselves. However, if we assert a priori that all
            // stores are live, then we will end up keeping both the
            // stores and the local variables, as the former take the
            // latter as arguments.
            //
            // When local variables become live, we will mark stores to
            // those variables as live as well. We will not do so before then.
            var localStores = GetLocalStores(graph);
            foreach (var set in localStores.Values)
            {
                liveValues.ExceptWith(set);
            }

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
                        HashSet<ValueTag> storeDependencies;
                        if (localStores.TryGetValue(value, out storeDependencies))
                        {
                            // A local variable has become live. We must mark any store instructions
                            // that depend on said local as live as well.
                            foreach (var dep in storeDependencies)
                            {
                                worklist.Enqueue(dep);
                            }
                        }
                    }
                }
            }

            return liveValues;
        }

        /// <summary>
        /// Creates a mapping of instructions that point to local variables to the
        /// stores that operate on those local variables.
        /// </summary>
        /// <param name="graph">A control flow graph.</param>
        private static Dictionary<ValueTag, HashSet<ValueTag>> GetLocalStores(FlowGraph graph)
        {
            var results = new Dictionary<ValueTag, HashSet<ValueTag>>();
            foreach (var instruction in graph.NamedInstructions)
            {
                if (instruction.Prototype is StorePrototype)
                {
                    var pointer = ((StorePrototype)instruction.Prototype).GetPointer(instruction.Instruction);
                    IEnumerable<ValueTag> localInstructions;
                    if (TryRecognizeAsLocal(pointer, graph, out localInstructions))
                    {
                        foreach (var item in localInstructions)
                        {
                            HashSet<ValueTag> localDeps;
                            if (!results.TryGetValue(item, out localDeps))
                            {
                                results[item] = localDeps = new HashSet<ValueTag>();
                            }
                            localDeps.Add(instruction);
                        }
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Tries to recognize a value as a pointer to an alloca.
        /// </summary>
        /// <param name="pointer">A value to inspect.</param>
        /// <param name="graph">A control flow graph that defines <paramref name="pointer"/>.</param>
        /// <param name="localInstructions">
        /// A chain of pointers derived from a local.
        /// The chain starts at <paramref name="pointer"/> and ends at an alloca.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="pointer"/> is recognized as an alloca or a pointer
        /// derived from an alloca; otherwise, <c>false</c>.
        /// </returns>
        private static bool TryRecognizeAsLocal(
            ValueTag pointer,
            FlowGraph graph,
            out IEnumerable<ValueTag> localInstructions)
        {
            NamedInstruction instruction;
            if (graph.TryGetInstruction(pointer, out instruction))
            {
                if (instruction.Prototype is AllocaPrototype
                    || instruction.Prototype is AllocaArrayPrototype)
                {
                    localInstructions = new[] { pointer };
                    return true;
                }
                else if (instruction.Prototype is CopyPrototype
                    || instruction.Prototype is ReinterpretCastPrototype
                    || instruction.Prototype is GetFieldPointerPrototype)
                {
                    if (TryRecognizeAsLocal(instruction.Arguments[0], graph, out localInstructions))
                    {
                        localInstructions = new[] { pointer }.Concat(localInstructions);
                        return true;
                    }
                }
            }
            localInstructions = null;
            return false;
        }
    }
}
