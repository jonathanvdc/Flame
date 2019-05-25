using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A cache for a flow graph analysis. It retains
    /// the last analysis' result and the changes that
    /// have been applied to the graph since then.
    ///
    /// A flow graph analysis cache presents a thread-safe
    /// view of an analysis' result. The analysis itself is
    /// performed lazily.
    /// </summary>
    internal abstract class FlowGraphAnalysisCache
    {
        /// <summary>
        /// Updates this flow graph analysis cache with a tweak to
        /// the graph. The update is not performed in place: instead,
        /// a derived cache is created.
        /// </summary>
        /// <param name="update">A tweak to the graph.</param>
        /// <returns>
        /// A flow graph analysis cache that incorporates the update.
        /// </returns>
        /// <remarks>This method is thread-safe.</remarks>
        public abstract FlowGraphAnalysisCache Update(FlowGraphUpdate update);

        /// <summary>
        /// Gets the result of the analysis managed by this cache.
        /// </summary>
        /// <param name="graph">The graph to analyze.</param>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <returns>The analysis' result.</returns>
        /// <remarks>This method is thread-safe.</remarks>
        public abstract T GetResultAs<T>(FlowGraph graph);

        /// <summary>
        /// Gets the analysis managed by this cache.
        /// </summary>
        /// <typeparam name="T">The type of the analysis' results.</typeparam>
        /// <returns>The analysis.</returns>
        /// <remarks>This method is thread-safe.</remarks>
        public abstract IFlowGraphAnalysis<T> GetAnalysis<T>();
    }

    /// <summary>
    /// A cache for a flow graph analysis. It retains
    /// the last analysis' result and the changes that
    /// have been applied to the graph since then.
    ///
    /// A flow graph analysis cache presents a thread-safe
    /// view of an analysis' result. The analysis itself is
    /// performed lazily.
    /// </summary>
    internal sealed class FlowGraphAnalysisCache<T> : FlowGraphAnalysisCache
    {
        private FlowGraphAnalysisCache()
        { }

        /// <summary>
        /// Creates an empty flow graph analysis cache from
        /// an analysis.
        /// </summary>
        /// <param name="analysis">The analysis.</param>
        public FlowGraphAnalysisCache(IFlowGraphAnalysis<T> analysis)
        {
            this.analysis = analysis;
            this.resultLock = new ReaderWriterLockSlim();
        }

        private sealed class Box<TVal>
        {
            public TVal value;
        }

        /// <summary>
        /// The result of the the analysis.
        /// </summary>
        private Box<T> cachedResult;

        /// <summary>
        /// The update applied to the graph since the
        /// parent cache.
        /// </summary>
        private FlowGraphUpdate update;

        /// <summary>
        /// The analysis cache that is this analysis cache's
        /// parent.
        /// </summary>
        private FlowGraphAnalysisCache<T> parentCache;

        /// <summary>
        /// The analysis managed by this analysis cache.
        /// </summary>
        private IFlowGraphAnalysis<T> analysis;

        /// <summary>
        /// A synchronization object for the flow graph analysis cache.
        /// </summary>
        private ReaderWriterLockSlim resultLock;

        /// <inheritdoc/>
        public override FlowGraphAnalysisCache Update(FlowGraphUpdate update)
        {
            var result = new FlowGraphAnalysisCache<T>();
            result.update = update;
            result.parentCache = this;
            result.analysis = this.analysis;
            result.resultLock = this.resultLock;
            return result;
        }

        /// <inheritdoc/>
        public override IFlowGraphAnalysis<TResult> GetAnalysis<TResult>()
        {
            return (IFlowGraphAnalysis<TResult>)analysis;
        }

        /// <inheritdoc/>
        public override TResult GetResultAs<TResult>(FlowGraph graph)
        {
            return (TResult)(object)GetResult(graph);
        }

        /// <summary>
        /// Gets the result of the analysis managed by this cache.
        /// </summary>
        /// <param name="graph">The graph to analyze.</param>
        /// <returns>The analysis' result.</returns>
        /// <remarks>This method is thread-safe.</remarks>
        public T GetResult(FlowGraph graph)
        {
            // Happy path: result has already been computed
            // and can be efficiently accessed by multiple threads
            // at the same time by entering a read-only lock.
            resultLock.EnterReadLock();
            try
            {
                if (cachedResult != null)
                {
                    return cachedResult.value;
                }
            }
            finally
            {
                resultLock.ExitReadLock();
            }

            // Result has not been computed yet. We'll need to
            // enter a write lock and maybe even compute it.
            resultLock.EnterWriteLock();
            try
            {
                // Before we actually compute the result, we've got to
                // check if another thread beat us to the punch,
                if (cachedResult != null)
                {
                    return cachedResult.value;
                }

                // Okay, guess we'll compute it ourselves then.
                var result = ComputeResult(graph);
                cachedResult = new Box<T>
                {
                    value = result
                };

                // Set the parent cache to `null` so we don't create
                // a memory leak for no good reason.
                parentCache = null;
                return result;
            }
            finally
            {
                resultLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Computes the analysis' result for a particular graph.
        /// </summary>
        /// <param name="graph">The graph to compute the result for.</param>
        /// <returns>The analysis' result.</returns>
        /// <remarks>This method is not thread-safe.</remarks>
        private T ComputeResult(FlowGraph graph)
        {
            // Our first order of business is to figure out if some ancestor
            // of this analysis cache has already computed a result. And if so,
            // we want to compose a list of all updates since that result
            // was computed.
            var updatesSinceResult = new List<FlowGraphUpdate>();
            Box<T> oldResult = null;
            var ancestor = this;
            while (true)
            {
                oldResult = ancestor.cachedResult;
                if (oldResult != null)
                {
                    // We found a stale result. No need to
                    // keep on looking.
                    break;
                }

                if (ancestor.parentCache == null)
                {
                    // We didn't find a stale result and we can out
                    // of ancestor caches to visit. Stop here.
                    break;
                }

                updatesSinceResult.Add(ancestor.update);
                ancestor = ancestor.parentCache;
            }

            if (oldResult == null)
            {
                // No parent cache has computed a result yet.
                // Have the analysis analyze the graph from scratch.
                return analysis.Analyze(graph);
            }
            else
            {
                // Reverse the list of updates because we composed it
                // in reverse.
                updatesSinceResult.Reverse();

                // We have a stale result. Maybe the analysis can
                // update it. Or maybe it'll just compute the analysis
                // from scratch. Either way, our job here is done.
                // A simple call to `AnalyzeWithUpdates` will suffice.
                return analysis.AnalyzeWithUpdates(graph, oldResult.value, updatesSinceResult);
            }
        }
    }
}
