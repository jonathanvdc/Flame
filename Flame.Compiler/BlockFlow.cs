using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// Describes control flow at the end of a basic block.
    /// </summary>
    public abstract class BlockFlow
    {
        /// <summary>
        /// Gets a list of values this flow takes as arguments.
        /// </summary>
        /// <returns>The values taken as arguments.</returns>
        public abstract IReadOnlyList<ValueTag> Arguments { get; }

        /// <summary>
        /// Replaces this flow's arguments with a particular
        /// list of arguments.
        /// </summary>
        /// <param name="arguments">The new arguments.</param>
        /// <returns>A new flow.</returns>
        public abstract BlockFlow WithArguments(IReadOnlyList<ValueTag> arguments);

        /// <summary>
        /// Gets a list of basic blocks to which this flow may
        /// transfer control.
        /// </summary>
        /// <returns>A list of potential target blocks.</returns>
        public abstract IReadOnlyList<BasicBlockTag> Targets { get; }

        /// <summary>
        /// Replaces this flow's targets with a particular
        /// list of targets.
        /// </summary>
        /// <param name="targets">The new targets.</param>
        /// <returns>A new flow.</returns>
        public abstract BlockFlow WithTargets(IReadOnlyList<BasicBlockTag> targets);
    }
}