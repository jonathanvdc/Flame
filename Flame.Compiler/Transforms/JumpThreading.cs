namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that tries to eliminate repeated
    /// jumps between blocks.
    /// </summary>
    public sealed class JumpThreading : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates an instance of the jump threading transform.
        /// </summary>
        /// <param name="includeSwitches">
        /// Tells if switches are also eligible for jump threading.
        /// If set to <c>true</c>, then switches that branch to
        /// other switches can be merged and jumps to switches can
        /// be replaced with the switch itself.
        /// </param>
        public JumpThreading(bool includeSwitches = true)
        {
            this.IncludeSwitches = includeSwitches;
        }

        /// <summary>
        /// Tells if switches are also eligible for jump threading.
        /// If set to <c>true</c>, then switches that branch to
        /// other switches can be merged and jumps to switches can
        /// be replaced with the switch itself.
        /// </summary>
        /// <value>
        /// <c>true</c> if switches are eligible for jump threading; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeSwitches { get; private set; }

        /// <summary>
        /// Applies the jump threading optimization to a flow graph.
        /// </summary>
        /// <param name="graph">The flow graph to rewrite.</param>
        /// <returns>An optimized flow graph.</returns>
        public override FlowGraph Apply(FlowGraph graph)
        {
            throw new System.NotImplementedException();
        }
    }
}
