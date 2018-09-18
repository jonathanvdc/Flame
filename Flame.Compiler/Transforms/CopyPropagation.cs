using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Instructions;
using Flame.Constants;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// The copy propagation transform, which replaces references
    /// to values that are merely copies of values with the
    /// copied values themselves.
    /// </summary>
    public static class CopyPropagation
    {
        /// <summary>
        /// Propagates copies in a flow graph.
        /// </summary>
        /// <param name="graph">
        /// The graph to transform.
        /// </param>
        /// <returns>
        /// A transformed graph.
        /// </returns>
        public static FlowGraph Apply(FlowGraph graph)
        {
            // We'll first create a mapping of values to the
            // values they copy. Then we'll use that to
            // rewrite uses.
            var copyMap = FindCopies(graph);

            var graphBuilder = graph.ToBuilder();

            // Undefined values are encoded as `null` values.
            // We will first materialize them as `default`
            // constants and then replace uses.
            var entryPoint = graphBuilder.GetBasicBlock(graphBuilder.EntryPointTag);
            var materializedReplacements = new Dictionary<ValueTag, ValueTag>();
            foreach (var pair in copyMap)
            {
                if (pair.Value == null)
                {
                    materializedReplacements[pair.Key] = entryPoint.InsertInstruction(
                        0,
                        Instruction.CreateConstant(
                            DefaultConstant.Instance,
                            graphBuilder.GetValueType(pair.Key))).Tag;
                }
                else
                {
                    materializedReplacements[pair.Key] = pair.Value;
                }
            }

            graphBuilder.ReplaceUses(materializedReplacements);
            return graphBuilder.ToImmutable();
        }

        /// <summary>
        /// Creates a mapping of copy values to the values
        /// they copy.
        /// </summary>
        /// <param name="graph">
        /// The graph to analyze.
        /// </param>
        /// <returns>
        /// A mapping of copy values to copied values.
        /// </returns>
        private static IReadOnlyDictionary<ValueTag, ValueTag> FindCopies(
            FlowGraph graph)
        {
            // We want to propagate three types of copies here:
            //
            //   1. `copy` instructions,
            //   2. `store` instructions' result values, and
            //   3. block parameters.
            //
            // The first two types of copies are easy to propagate,
            // but block parameters are slightly trickier: they
            // we will only eliminate them if they are trivial.

            var copyMap = new Dictionary<ValueTag, ValueTag>();

            // Handle instructions.
            foreach (var selection in graph.Instructions)
            {
                var instruction = selection.Instruction;
                var proto = instruction.Prototype;
                if (proto is CopyPrototype)
                {
                    copyMap[selection.Tag] = ((CopyPrototype)proto).GetCopiedValue(instruction);
                }
                else if (proto is StorePrototype)
                {
                    copyMap[selection.Tag] = ((StorePrototype)proto).GetValue(instruction);
                }
            }

            // Populate data structures for phis.
            var phiArgs = new Dictionary<ValueTag, HashSet<ValueTag>>();
            var phiUsers = new Dictionary<ValueTag, HashSet<ValueTag>>();
            var specialPhis = new HashSet<ValueTag>();
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var param in block.ParameterTags)
                {
                    phiArgs[param] = new HashSet<ValueTag>();
                    phiUsers[param] = new HashSet<ValueTag>();
                    if (block.IsEntryPoint)
                    {
                        specialPhis.Add(param);
                    }
                }
            }

            foreach (var block in graph.BasicBlocks)
            {
                foreach (var branch in block.Flow.Branches)
                {
                    foreach (var pair in DeadValueElimination
                        .ZipArgumentsAndParameters(branch, graph))
                    {
                        if (pair.Value.IsValue)
                        {
                            var val = pair.Value.ValueOrNull;
                            phiArgs[pair.Key].Add(val);
                            if (phiUsers.ContainsKey(val))
                            {
                                phiUsers[val].Add(pair.Key);
                            }
                        }
                        else
                        {
                            specialPhis.Add(pair.Key);
                        }
                    }
                }
            }

            // Now run the actual trivial phi elimination
            // algorithm.
            foreach (var tag in phiArgs.Keys)
            {
                TryRemoveTrivialPhi(tag, phiArgs, phiUsers, specialPhis, copyMap);
            }

            // Propagate chains of copied values.
            foreach (var key in copyMap.Keys.ToArray())
            {
                GetActualCopy(key, copyMap);
            }

            return copyMap;
        }

        private static void TryRemoveTrivialPhi(
            ValueTag phi,
            Dictionary<ValueTag, HashSet<ValueTag>> phiArgs,
            Dictionary<ValueTag, HashSet<ValueTag>> phiUsers,
            HashSet<ValueTag> specialPhis,
            Dictionary<ValueTag, ValueTag> copyMap)
        {
            // This algorithm is based on the `tryRemoveTrivialPhi` algorithm as described
            // by M. Braun et al in Simple and Efficient Construction of Static Single
            // Assignment Form
            // (https://pp.info.uni-karlsruhe.de/uploads/publikationen/braun13cc.pdf).

            if (phi == null || specialPhis.Contains(phi) || copyMap.ContainsKey(phi))
            {
                // Never ever eliminate special phis.
                return;
            }

            ValueTag same = null;
            foreach (var arg in phiArgs[phi])
            {
                var actualArg = GetActualCopy(arg, copyMap);
                if (actualArg == same || actualArg == phi)
                {
                    continue;
                }
                else if (same != null)
                {
                    // The phi merges at least two values, which
                    // makes it non-trivial.
                    return;
                }
                same = actualArg;
            }

            // Reroute all uses of `phi` to `same`.
            copyMap[phi] = same;

            // Recurse on `phi` users, which may have become trivial.
            foreach (var use in phiUsers[phi])
            {
                TryRemoveTrivialPhi(
                    GetActualCopy(use, copyMap),
                    phiArgs,
                    phiUsers,
                    specialPhis,
                    copyMap);
            }
        }

        private static ValueTag GetActualCopy(ValueTag key, Dictionary<ValueTag, ValueTag> copyMap)
        {
            ValueTag copy;
            if (copyMap.TryGetValue(key, out copy))
            {
                var actualCopy = GetActualCopy(copy, copyMap);
                if (actualCopy != copy)
                {
                    copyMap[key] = actualCopy;
                }
                return actualCopy;
            }
            else
            {
                return key;
            }
        }
    }
}
