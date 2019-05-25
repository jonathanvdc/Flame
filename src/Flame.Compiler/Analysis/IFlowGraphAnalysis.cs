using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// An analysis on a flow graph.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the analysis' result.
    /// </typeparam>
    public interface IFlowGraphAnalysis<T>
    {
        /// <summary>
        /// Analyzes a flow graph from scratch.
        /// </summary>
        /// <param name="graph">The flow graph to analyze.</param>
        /// <returns>The analysis' output.</returns>
        T Analyze(FlowGraph graph);

        /// <summary>
        /// Analyzes a flow graph based on the flow graph, the
        /// previous result, and a list of updates that were
        /// applied to the graph since the previous result.
        /// </summary>
        /// <param name="graph">
        /// The current version of the flow graph to analyze.
        /// </param>
        /// <param name="previousResult">
        /// A previous result produced by this analysis.
        /// </param>
        /// <param name="updates">
        /// A list of updates that were applied to the flow graph
        /// since the previous result was computed.
        /// </param>
        /// <returns>
        /// The analysis' output, which must be equal to a call to
        /// <c>Analyze</c>.
        /// </returns>
        T AnalyzeWithUpdates(
            FlowGraph graph,
            T previousResult,
            IReadOnlyList<FlowGraphUpdate> updates);
    }
}
