using System;
using System.Collections.Generic;
using Flame.TypeSystem;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Manages a collection of default control-flow graph analyses, which
    /// are used when no analysis is explicitly added to a graph.
    /// </summary>
    public static class DefaultAnalyses
    {
        static DefaultAnalyses()
        {
            defaults = new Dictionary<Type, Func<FlowGraph, FlowGraphAnalysisCache>>();

            Register(ValueUseAnalysis.Instance);
            Register(new EffectfulInstructionAnalysis());
            Register(NullabilityAnalysis.Instance);
            Register(LazyBlockReachabilityAnalysis.Instance);
            Register(ConservativeInstructionOrderingAnalysis.Instance);
            Register(PredecessorAnalysis.Instance);
            Register(RelatedValueAnalysis.Instance);
            Register(InterferenceGraphAnalysis.Instance);
            Register(LivenessAnalysis.Instance);
            Register(new ConstantAnalysis<StrictExceptionDelayability>(StrictExceptionDelayability.Instance));
            Register(ValueNumberingAnalysis.Instance);
            Register(DominatorTreeAnalysis.Instance);
            Register(new ConstantAnalysis<TrivialAliasAnalysisResult>(TrivialAliasAnalysisResult.Instance));
            Register(LocalMemorySSAAnalysis.Instance);
            Register(graph =>
                new ConstantAnalysis<AccessRules>(
                    new StandardAccessRules(
                        graph.GetAnalysisResult<SubtypingRules>())));

            Register(new ConstantAnalysis<PrototypeExceptionSpecs>(RuleBasedPrototypeExceptionSpecs.Default));
            Register(ReifiedInstructionExceptionAnalysis.Instance);
            Register(new ConstantAnalysis<PrototypeMemorySpecs>(RuleBasedPrototypeMemorySpecs.Default));
        }

        private static readonly Dictionary<Type, Func<FlowGraph, FlowGraphAnalysisCache>> defaults;

        /// <summary>
        /// Registers a default analysis for a particular type of analysis result.
        /// </summary>
        /// <param name="analysis">
        /// The analysis to register.
        /// </param>
        /// <typeparam name="T">The type of result produced by the analysis.</typeparam>
        public static void Register<T>(IFlowGraphAnalysis<T> analysis)
        {
            Func<FlowGraph, FlowGraphAnalysisCache> createCache =
                graph => new FlowGraphAnalysisCache<T>(analysis);

            foreach (var type in MacroAnalysisCache.GetAssignableTypes(typeof(T)))
            {
                defaults[type] = createCache;
            }
        }

        /// <summary>
        /// Registers a function that creates a default analysis for a particular type
        /// of analysis result.
        /// </summary>
        /// <param name="createAnalysis">
        /// A function that creates an analysis based on a control-flow graph.
        /// </param>
        /// <typeparam name="T">The type of result produced by the analysis.</typeparam>
        public static void Register<T>(Func<FlowGraph, IFlowGraphAnalysis<T>> createAnalysis)
        {
            Func<FlowGraph, FlowGraphAnalysisCache> createCache =
                graph => new FlowGraphAnalysisCache<T>(createAnalysis(graph));

            foreach (var type in MacroAnalysisCache.GetAssignableTypes(typeof(T)))
            {
                defaults[type] = createCache;
            }
        }

        /// <summary>
        /// Creates the default flow graph analysis cache for a particular type of analysis,
        /// given a particular control-flow graph. Returns a Boolean that tells if this
        /// function was successful.
        /// </summary>
        /// <param name="type">The type of analysis to get an analysis cache for.</param>
        /// <param name="graph">The graph to tailor the analysis to.</param>
        /// <param name="result">An analysis cache, if one can be created.</param>
        /// <returns>
        /// <c>true</c> if a default analysis is registered for <paramref name="type"/>;
        /// otherwise, <c>false</c>.</returns>
        internal static bool TryGetDefaultAnalysisCache(
            Type type, FlowGraph graph, out FlowGraphAnalysisCache result)
        {
            Func<FlowGraph, FlowGraphAnalysisCache> create;
            if (defaults.TryGetValue(type, out create))
            {
                result = create(graph);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}
