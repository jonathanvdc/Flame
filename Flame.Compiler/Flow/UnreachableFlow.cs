using System.Collections.Generic;
using Flame.Collections;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// Control flow that marks the end of a basic block as unreachable.
    /// </summary>
    public sealed class UnreachableFlow : BlockFlow
    {
        private UnreachableFlow()
        { }

        /// <summary>
        /// Gets an instance of unreachable flow.
        /// </summary>
        /// <returns>Unreachable flow.</returns>
        public static readonly UnreachableFlow Instance = new UnreachableFlow();

        /// <inheritdoc/>
        public override IReadOnlyList<ValueTag> Arguments => EmptyArray<ValueTag>.Value;

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => EmptyArray<Branch>.Value;

        /// <inheritdoc/>
        public override BlockFlow WithArguments(IReadOnlyList<ValueTag> arguments)
        {
            ContractHelpers.Assert(arguments.Count == 0, "Unreachable flow does not take any arguments.");
            return this;
        }

        /// <inheritdoc/>
        public override BlockFlow WithBranches(IReadOnlyList<Branch> branches)
        {
            ContractHelpers.Assert(branches.Count == 0, "Unreachable flow does not take any branches.");
            return this;
        }
    }
}