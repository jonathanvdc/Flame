using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A symmetric relation that consists of all pairs of values
    /// that are direct copies of each other.
    ///
    /// A value is deemed a copy of another value if the former is
    /// produced by a copy instruction that references the latter or
    /// if the former is a block parameter and there is a branch that
    /// specifies the latter as the argument to the former.
    ///
    /// This information can be useful for the purpose of register
    /// allocation: allocating two related values to the same register
    /// will elide a copy.
    /// </summary>
    public struct RelatedValues
    {
        internal RelatedValues(SymmetricRelation<ValueTag> relation)
        {
            this.relation = relation;
        }

        private SymmetricRelation<ValueTag> relation;

        /// <summary>
        /// Tests if two values are related.
        /// </summary>
        /// <param name="first">
        /// A first value.
        /// </param>
        /// <param name="second">
        /// A second value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the values are related; otherwise, <c>false</c>.
        /// </returns>
        public bool AreRelated(ValueTag first, ValueTag second)
        {
            return relation.Contains(first, second);
        }

        /// <summary>
        /// Gets the set of all values related to a particular value.
        /// </summary>
        /// <param name="value">The related value.</param>
        /// <returns>The set of related values.</returns>
        public IEnumerable<ValueTag> GetRelatedValues(ValueTag value)
        {
            return relation.GetAll(value);
        }
    }

    /// <summary>
    /// An analysis that computes the related value--relation for graphs.
    /// </summary>
    public sealed class RelatedValueAnalysis : IFlowGraphAnalysis<RelatedValues>
    {
        private RelatedValueAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the related value analysis.
        /// </summary>
        /// <returns>An instance of the related value analysis.</returns>
        public static readonly RelatedValueAnalysis Instance = new RelatedValueAnalysis();

        /// <inheritdoc/>
        public RelatedValues Analyze(FlowGraph graph)
        {
            var relation = new SymmetricRelation<ValueTag>();

            // Examine direct copies.
            foreach (var selection in graph.NamedInstructions)
            {
                var instruction = selection.Instruction;
                var prototype = instruction.Prototype as CopyPrototype;
                if (prototype != null)
                {
                    relation.Add(
                        selection.Tag,
                        prototype.GetCopiedValue(instruction));
                }
            }

            // Examine copies produced by branches.
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var branch in block.Flow.Branches)
                {
                    var parameters = graph.GetBasicBlock(branch.Target).Parameters;
                    foreach (var pair in branch.Arguments.Zip(parameters, Tuple.Create))
                    {
                        if (pair.Item1.IsValue)
                        {
                            relation.Add(
                                pair.Item1.ValueOrNull,
                                pair.Item2.Tag);
                        }
                    }
                }
            }
            return new RelatedValues(relation);
        }

        /// <inheritdoc/>
        public RelatedValues AnalyzeWithUpdates(
            FlowGraph graph,
            RelatedValues previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
