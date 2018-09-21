using System.Collections.Generic;
using System.Linq;

namespace Flame.Compiler
{
    /// <summary>
    /// Describes control flow at the end of a basic block.
    /// </summary>
    public abstract class BlockFlow
    {
        /// <summary>
        /// Gets a list of inner instructions for this block flow.
        /// </summary>
        /// <returns>The inner instructions.</returns>
        public abstract IReadOnlyList<Instruction> Instructions { get; }

        /// <summary>
        /// Replaces this flow's inner instructions.
        /// </summary>
        /// <param name="instructions">The new instructions.</param>
        /// <returns>A new flow.</returns>
        public abstract BlockFlow WithInstructions(IReadOnlyList<Instruction> instructions);

        /// <summary>
        /// Gets a list of branches this flow may take.
        /// </summary>
        /// <returns>A list of potential branches.</returns>
        public abstract IReadOnlyList<Branch> Branches { get; }

        /// <summary>
        /// Replaces this flow's branches with a particular
        /// list of branches.
        /// </summary>
        /// <param name="branches">The new branches.</param>
        /// <returns>A new flow.</returns>
        public abstract BlockFlow WithBranches(IReadOnlyList<Branch> branches);

        /// <summary>
        /// Gets a list of each branch's target.
        /// </summary>
        /// <value>A list of branch targets.</value>
        public IEnumerable<BasicBlockTag> BranchTargets
        {
            get
            {
                return Branches.Select(branch => branch.Target);
            }
        }
    }
}
