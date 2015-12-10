using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flame.Recompilation
{
    using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;

    /// <summary>
    /// Defines an aggregate root pass, which applies two
    /// root passes to the same argument, and then concatenates
    /// the resulting root member sequences together.
    /// </summary>
    public sealed class AggregateRootPass : IRootPass
    {
        /// <summary>
        /// Creates an aggregate root pass from the given root passes.
        /// </summary>
        /// <param name="First"></param>
        /// <param name="Second"></param>
        public AggregateRootPass(IRootPass First, IRootPass Second)
        {
            this.First = First;
            this.Second = Second;
        }

        /// <summary>
        /// Gets the first root pass to apply to
        /// the method and its body.
        /// </summary>
        public IRootPass First { get; private set; }

        /// <summary>
        /// Gets the second root pass to apply to
        /// the method and its body.
        /// </summary>
        public IRootPass Second { get; private set; }

        /// <summary>
        /// Applies the root pass to the given body pass argument
        /// by applying both root passes to it, and then concatenating
        /// the resulting root member sequences together.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public IEnumerable<IMember> Apply(BodyPassArgument Value)
        {
            return First.Apply(Value).Concat(Second.Apply(Value));
        }
    }
}
