using System.Threading.Tasks;
using Flame.Compiler.Pipeline;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// Describes an intraprocedural optimization: an optimization
    /// that considers a method implementation only and does not
    /// rely on the implementation of other methods.
    /// </summary>
    public abstract class IntraproceduralOptimization : Optimization
    {
        /// <inheritdoc/>
        public override bool IsCheckpoint => false;

        /// <inheritdoc/>
        public override Task<MethodBody> ApplyAsync(
            MethodBody body,
            OptimizationState state)
        {
            return Task.FromResult(body.WithImplementation(Apply(body.Implementation)));
        }

        /// <summary>
        /// Applies this intraprocedural optimization to a flow graph.
        /// </summary>
        /// <param name="graph">The flow graph to transform.</param>
        /// <returns>A transformed flow graph.</returns>
        public abstract FlowGraph Apply(FlowGraph graph);
    }
}
