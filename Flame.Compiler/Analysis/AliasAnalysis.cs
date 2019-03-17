namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Enumerates possible aliasing relations between pointers.
    /// </summary>
    public enum Aliasing
    {
        /// <summary>
        /// The pointers must alias.
        /// </summary>
        MustAlias,

        /// <summary>
        /// The pointers cannot alias.
        /// </summary>
        NoAlias,

        /// <summary>
        /// The pointers may alias.
        /// </summary>
        MayAlias
    }

    /// <summary>
    /// A data structure that captures the result of applying alias
    /// analysis to a control-flow graph.
    /// </summary>
    public abstract class AliasAnalysisResult
    {
        /// <summary>
        /// Gets the aliasing relation between two pointers.
        /// </summary>
        /// <param name="first">
        /// The first pointer value to examine.
        /// </param>
        /// <param name="second">
        /// The second pointer value to examine.
        /// </param>
        /// <returns>
        /// A conservative approximation of the aliasing relation between
        /// <paramref name="first"/> and <paramref name="second"/>.
        /// </returns>
        public abstract Aliasing GetAliasing(ValueTag first, ValueTag second);
    }

    /// <summary>
    /// A truly trivial alias analysis result implementation: all values
    /// are deemed to must-alias themselves and may-alias all other values.
    /// </summary>
    public sealed class TrivialAliasAnalysisResult : AliasAnalysisResult
    {
        private TrivialAliasAnalysisResult()
        { }

        /// <summary>
        /// An instance of the trivial alias analysis result.
        /// </summary>
        public static readonly TrivialAliasAnalysisResult Instance =
            new TrivialAliasAnalysisResult();

        /// <inheritdoc/>
        public override Aliasing GetAliasing(ValueTag first, ValueTag second)
        {
            return first == second ? Aliasing.MustAlias : Aliasing.MayAlias;
        }
    }
}
