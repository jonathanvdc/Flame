using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler.Instructions;
using Flame.Constants;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// The copy propagation transform, which replaces references
    /// to values that are merely copies of values with the
    /// copied values themselves.
    /// </summary>
    public sealed class CopyPropagation : IntraproceduralOptimization
    {
        private CopyPropagation()
        { }

        /// <summary>
        /// An instance of the copy propagation transform.
        /// </summary>
        public static readonly CopyPropagation Instance = new CopyPropagation();

        /// <summary>
        /// Propagates copies in a flow graph.
        /// </summary>
        /// <param name="graph">
        /// The graph to transform.
        /// </param>
        /// <returns>
        /// A transformed graph.
        /// </returns>
        public override FlowGraph Apply(FlowGraph graph)
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
                        Instruction.CreateDefaultConstant(graphBuilder.GetValueType(pair.Key)),
                        pair.Key.Name);
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
            foreach (var selection in graph.NamedInstructions)
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
                    foreach (var pair in branch.ZipArgumentsWithParameters(graph))
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

            // Run the trivial phi elimination algorithm.
            foreach (var tag in phiArgs.Keys)
            {
                TryRemoveTrivialPhi(tag, phiArgs, phiUsers, specialPhis, copyMap);
            }

            // Run the redundant phi elimination algorithm.
            var phiFunctions = new HashSet<ValueTag>(phiArgs.Keys);
            phiFunctions.ExceptWith(copyMap.Keys);
            RemoveRedundantPhis(phiFunctions, phiArgs, copyMap);

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
                if (actualArg == null || actualArg == same || actualArg == phi)
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

            // Reroute all uses of `phi` to `same`. If `same` is null,
            // then that means that the phi has zero arguments. It is
            // trivial, but not a "real" copy, so we'll just write `null`
            // to the copy map as well.
            copyMap[phi] = same;

            // Recurse on `phi` users, which may have become trivial.
            foreach (var use in phiUsers[phi])
            {
                var copy = GetActualCopy(use, copyMap);
                if (copy != null && phiArgs.ContainsKey(copy))
                {
                    // Be sure to check that the phi we want to eliminate
                    // is actually a phi. Things will go horribly wrong if
                    // we try to remove a "phi" that is actually an instruction
                    // instead of a block parameter.
                    TryRemoveTrivialPhi(
                        copy,
                        phiArgs,
                        phiUsers,
                        specialPhis,
                        copyMap);
                }
            }
        }

        private static void RemoveRedundantPhis(
            HashSet<ValueTag> phiFunctions,
            Dictionary<ValueTag, HashSet<ValueTag>> phiArgs,
            Dictionary<ValueTag, ValueTag> copyMap)
        {
            // This algorithm is based on the `removeRedundantPhis` algorithm as described
            // by M. Braun et al in Simple and Efficient Construction of Static Single
            // Assignment Form
            // (https://pp.info.uni-karlsruhe.de/uploads/publikationen/braun13cc.pdf).

            // Run the redundant phi elimination algorithm.
            foreach (var scc in ComputePhiSCCs(phiFunctions, phiArgs, copyMap))
            {
                ProcessPhiSCC(scc, phiArgs, copyMap);
            }
        }

        private static IReadOnlyList<HashSet<ValueTag>> ComputePhiSCCs(
            HashSet<ValueTag> phiFunctions,
            Dictionary<ValueTag, HashSet<ValueTag>> phiArgs,
            Dictionary<ValueTag, ValueTag> copyMap)
        {
            // Computes all phi SCCs in the subgraph induced by phis.
            return StronglyConnectedComponents.Compute(
                phiFunctions,
                arg => phiArgs[arg].Where(phiFunctions.Contains));
        }

        private static void ProcessPhiSCC(
            HashSet<ValueTag> scc,
            Dictionary<ValueTag, HashSet<ValueTag>> phiArgs,
            Dictionary<ValueTag, ValueTag> copyMap)
        {
            if (scc.Count == 1)
            {
                // We already handled trivial phis.
                return;
            }

            var inner = new HashSet<ValueTag>();
            var outerOps = new HashSet<ValueTag>();
            foreach (var phi in scc)
            {
                bool isInner = true;
                foreach (var oldOperand in phiArgs[phi])
                {
                    var operand = GetActualCopy(oldOperand, copyMap);
                    if (operand != null && !scc.Contains(operand))
                    {
                        outerOps.Add(operand);
                        isInner = false;
                    }
                }
                if (isInner)
                {
                    inner.Add(phi);
                }
            }

            if (outerOps.Count == 1)
            {
                // Replace the entire SCC by the value.
                foreach (var phi in scc)
                {
                    copyMap[phi] = outerOps.Single();
                }
            }
            else if (outerOps.Count > 1)
            {
                // Recursively remove redundant phis in the SCC.
                RemoveRedundantPhis(inner, phiArgs, copyMap);
            }
        }

        private static ValueTag GetActualCopy(ValueTag key, Dictionary<ValueTag, ValueTag> copyMap)
        {
            ValueTag copy;
            if (key != null && copyMap.TryGetValue(key, out copy))
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
