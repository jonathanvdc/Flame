namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transform that can be applied to a flow graph.
    /// </summary>
    /// <remarks>
    /// Transforms may be specific to transform graphs. The
    /// context in which a transform can be legally applied
    /// is specified by the entity producing the transform.
    /// </remarks>
    public abstract class Transform
    {
        /// <summary>
        /// Applies the transform to a mutable flow graph.
        /// </summary>
        /// <param name="graph">A flow graph to rewrite.</param>
        public abstract void Apply(FlowGraphBuilder graph);
    }
}
