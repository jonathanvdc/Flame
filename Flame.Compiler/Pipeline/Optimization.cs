using System.Threading.Tasks;

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
        /// <returns>A task that produces an optimized method body.</returns>
        public abstract Task<MethodBody> ApplyAsync(
            MethodBody body,
            OptimizationState state);
    }

    /// <summary>
    /// A container for shared optimization state.
    /// </summary>
    public sealed class OptimizationState
    {
        /// <summary>
        /// Creates optimization state for a method.
        /// </summary>
        /// <param name="method">
        /// The method to create the optimization state for.
        /// </param>
        /// <param name="optimizer">
        /// The optimizer that is optimizing the method.
        /// </param>
        public OptimizationState(
            IMethod method,
            Optimizer optimizer)
        {
            this.Method = method;
            this.Optimizer = optimizer;
        }

        /// <summary>
        /// Gets the method that is being optimized.
        /// </summary>
        /// <value>A method.</value>
        public IMethod Method { get; private set; }

        /// <summary>
        /// Gets the optimizer that triggered the optimization. 
        /// </summary>
        /// <returns>A method optimizer.</returns>
        public Optimizer Optimizer { get; private set; }

        /// <summary>
        /// Asynchronously requests a method's body.
        /// </summary>
        /// <param name="method">The method whose body is requested.</param>
        /// <returns>The method's body.</returns>
        /// <remarks>
        /// The optimizer is free to return any method body that is
        /// semantically equivalent to <paramref name="method"/>'s body.
        /// This ranges from <paramref name="method"/>'s initial method
        /// body to its final optimized body.
        ///
        /// Which version of <paramref name="method"/>'s body is returned
        /// depends on the optimizer. The optimizer is expected to return
        /// a method body that is as optimized as possible given the
        /// constraints imposed by the optimizer's implementation.
        /// </remarks>
        public Task<MethodBody> GetBodyAsync(IMethod method)
        {
            return Optimizer.GetBodyAsync(method, this.Method);
        }
    }
}
