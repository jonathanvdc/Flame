using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A macroscopic analysis cache: a manager of sorts for analysis caches.
    /// </summary>
    internal sealed class MacroAnalysisCache
    {
        /// <summary>
        /// Creates an empty macro analysis cache.
        /// </summary>
        public MacroAnalysisCache()
        {
            this.distinctCaches = new List<FlowGraphAnalysisCache>();
            this.cacheIndices = ImmutableDictionary.Create<Type, int>();
            this.cacheRefCounts = ImmutableDictionary.Create<int, int>();
        }

        private MacroAnalysisCache(
            List<FlowGraphAnalysisCache> distinctCaches,
            ImmutableDictionary<Type, int> cacheIndices,
            ImmutableDictionary<int, int> cacheRefCounts)
        {
            this.distinctCaches = distinctCaches;
            this.cacheIndices = cacheIndices;
            this.cacheRefCounts = cacheRefCounts;
        }

        /// <summary>
        /// A list of all distinct analysis caches.
        /// </summary>
        private List<FlowGraphAnalysisCache> distinctCaches;

        /// <summary>
        /// A mapping of types to the analysis caches that
        /// perform analyses for those types. The analyis
        /// caches are encoded as indices into the
        /// `distinctCaches` list.
        /// </summary>
        private ImmutableDictionary<Type, int> cacheIndices;

        /// <summary>
        /// A reference count for each flow graph analysis cache
        /// managed by this macro cache. The keys of this dictionary
        /// are indices into the `distinctCaches` list. The values
        /// are reference counts.
        /// </summary>
        private ImmutableDictionary<int, int> cacheRefCounts;

        /// <summary>
        /// Updates this macro analysis cache with a tweak to
        /// the graph. The update is not performed in place: instead,
        /// a derived cache is created.
        /// </summary>
        /// <param name="update">A tweak to the graph.</param>
        /// <returns>
        /// A macro analysis cache that incorporates the update.
        /// </returns>
        /// <remarks>This method is thread-safe.</remarks>
        public MacroAnalysisCache Update(FlowGraphUpdate update)
        {
            var newCaches = new List<FlowGraphAnalysisCache>();
            foreach (var cache in newCaches)
            {
                newCaches.Add(cache.Update(update));
            }
            return new MacroAnalysisCache(newCaches, cacheIndices, cacheRefCounts);
        }


        /// <summary>
        /// Tries to get an analysis result of a particular type.
        /// </summary>
        /// <param name="graph">
        /// The current flow graph.
        /// </param>
        /// <param name="result">
        /// The analysis result, if one can be fetched or computed.
        /// </param>
        /// <typeparam name="T">
        /// The type of analysis result to fetch or compute.
        /// </typeparam>
        /// <returns>
        /// <c>true</c> if there is an analyzer to compute the result;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetResultAs<T>(FlowGraph graph, out T result)
        {
            var t = typeof(T);
            int cacheIndex;
            if (cacheIndices.TryGetValue(t, out cacheIndex))
            {
                result = distinctCaches[cacheIndex].GetResultAs<T>(graph);
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// Gets an analysis result of a particular type. Throws
        /// an exception if there is no analyzer to compute the result.
        /// </summary>
        /// <param name="graph">
        /// The current flow graph.
        /// </param>
        /// <typeparam name="T">
        /// The type of analysis result to fetch or compute.
        /// </typeparam>
        /// <returns>An analysis result.</returns>
        public T GetResultAs<T>(FlowGraph graph)
        {
            T result;
            if (TryGetResultAs(graph, out result))
            {
                return result;
            }
            else
            {
                throw new NotSupportedException(
                    "No analysis was registered to produce results of type '" +
                    typeof(T).FullName + "'.");
            }
        }

        /// <summary>
        /// Tells if this macro analysis cache has an analysis
        /// that produces a particular type of result.
        /// </summary>
        /// <typeparam name="T">
        /// The type of analysis result that is sought.
        /// </typeparam>
        /// <returns>
        /// <c>true</c> if a registered analysis produces a result of type <c>T</c>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool HasAnalysisFor<T>()
        {
            return cacheIndices.ContainsKey(typeof(T));
        }
    }
}
