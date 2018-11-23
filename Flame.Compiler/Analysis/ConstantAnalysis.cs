using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// An analysis that always returns the same result.
    /// This kind of analysis is particularly useful for
    /// adding metadata to flow graphs that encode a
    /// (user-specified) configuration, such as the
    /// exception delayability policy.
    /// </summary>
    /// <typeparam name="T">
    /// The type of result returned by the analysis.
    /// </typeparam>
    public sealed class ConstantAnalysis<T> : IFlowGraphAnalysis<T>
    {
        /// <summary>
        /// Creates an analysis that returns a constant result.
        /// </summary>
        /// <param name="result">
        /// The constant result returned by the analysis.
        /// </param>
        public ConstantAnalysis(T result)
        {
            this.Result = result;
        }

        /// <summary>
        /// Gets the result that is returned when this analysis
        /// is coaxed into "analyzing" a flow graph.
        /// </summary>
        public T Result { get; private set; }

        /// <inheritdoc/>
        public T Analyze(FlowGraph graph)
        {
            return Result;
        }

        /// <inheritdoc/>
        public T AnalyzeWithUpdates(
            FlowGraph graph,
            T previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Result;
        }
    }
}
