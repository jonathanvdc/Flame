using System.Collections.Generic;
using Flame.Collections;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A symmetric relation that tells if there is some point at which
    /// two arbitrary values are both live.
    /// </summary>
    public sealed class InterferenceGraph
    {
        internal InterferenceGraph(
            SymmetricRelation<ValueTag> interferingValues)
        {
            this.interferingValues = interferingValues;
        }

        private SymmetricRelation<ValueTag> interferingValues;

        /// <summary>
        /// Gets the set of all values that interfere with a given value.
        /// </summary>
        /// <param name="value">
        /// The value to find the set of interfering values for.
        /// </param>
        /// <returns>A set of interfering values.</returns>
        public IEnumerable<ValueTag> GetInterferingValues(ValueTag value)
        {
            return interferingValues.GetAll(value);
        }

        /// <summary>
        /// Tests if one value interferes with another, that is,
        /// tests if there is at least one point in the program at
        /// which both values must exist simultaneously.
        /// </summary>
        /// <param name="first">
        /// The first value to consider.
        /// </param>
        /// <param name="second">
        /// The second value to consider.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="first"/> interferes with
        /// <paramref name="second"/>; otherwise; <c>false</c>.
        /// </returns>
        public bool InterferesWith(ValueTag first, ValueTag second)
        {
            return interferingValues.Contains(first, second);
        }
    }

    /// <summary>
    /// An analysis that constructs an interference graph.
    /// </summary>
    public sealed class InterferenceGraphAnalysis : IFlowGraphAnalysis<InterferenceGraph>
    {
        private InterferenceGraphAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the interference graph analysis.
        /// </summary>
        /// <returns>An instance of the interference graph analysis.</returns>
        public static readonly InterferenceGraphAnalysis Instance = new InterferenceGraphAnalysis();

        /// <inheritdoc/>
        public InterferenceGraph Analyze(FlowGraph graph)
        {
            var interference = new SymmetricRelation<ValueTag>();
            var liveness = graph.GetAnalysisResult<ValueLiveness>();
            foreach (var block in graph.BasicBlocks)
            {
                foreach (var group in liveness.GetLiveness(block.Tag).GetLiveValuesByIndex().Values)
                {
                    foreach (var first in group)
                    {
                        foreach (var second in group)
                        {
                            interference.Add(first, second);
                        }
                    }
                }
            }
            return new InterferenceGraph(interference);
        }

        /// <inheritdoc/>
        public InterferenceGraph AnalyzeWithUpdates(
            FlowGraph graph,
            InterferenceGraph previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
