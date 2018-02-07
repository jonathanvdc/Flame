using Flame.Collections;
using System.Collections.Generic;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// Control flow that unconditionally jumps to a particular branch.
    /// </summary>
    public sealed class JumpFlow : BlockFlow
    {
        /// <summary>
        /// Creates control flow that unconditionally jumps
        /// to a particular branch.
        /// </summary>
        /// <param name="branch">The branch to jump to.</param>
        public JumpFlow(Branch branch)
        {
            this.Branch = branch;
        }

        /// <summary>
        /// Gets the branch that is unconditionally taken by
        /// this flow.
        /// </summary>
        /// <returns>The jump branch.</returns>
        public Branch Branch { get; private set; }

        /// <inheritdoc/>
        public override IReadOnlyList<ValueTag> Arguments => EmptyArray<ValueTag>.Value;

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => new Branch[] { Branch };

        /// <inheritdoc/>
        public override BlockFlow WithArguments(IReadOnlyList<ValueTag> arguments)
        {
            ContractHelpers.Assert(arguments.Count == 0, "Jump flow does not take any arguments.");
            return this;
        }

        /// <inheritdoc/>
        public override BlockFlow WithBranches(IReadOnlyList<Branch> branches)
        {
            ContractHelpers.Assert(branches.Count == 1, "Jump flow takes exactly one branch.");
            var newBranch = branches[0];
            if (object.ReferenceEquals(newBranch, Branch))
            {
                return this;
            }
            else
            {
                return new JumpFlow(newBranch);
            }
        }
    }
}