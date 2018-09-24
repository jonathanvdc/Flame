namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// Describes a method body optimization.
    /// </summary>
    public abstract class Optimization
    {
        /// <summary>
        /// Tells if this optimization checkpoints its result.
        /// The optimizer takes care to always return the latest
        /// checkpointed method body is returned when a method's
        /// optimized method body is requested.
        /// </summary>
        /// <returns>Tells if this optimization performs a checkpoint.</returns>
        public abstract bool IsCheckpoint { get; }

        /// <summary>
        /// Applies the optimization to a method body.
        /// </summary>
        /// <param name="body">A method body holder to optimize.</param>
        /// <param name="state">State associated with optimizations.</param>
        public abstract MethodBody Apply(
            MethodBody body,
            OptimizationState state);
    }

    /// <summary>
    /// A container for shared optimization state.
    /// </summary>
    public sealed class OptimizationState
    {
        /// <summary>
        /// Gets the optimizer that triggered the optimization. 
        /// </summary>
        /// <returns>A method optimizer.</returns>
        public Optimizer Optimizer { get; private set; }
    }
}