using System.Collections.Generic;
using Flame.Collections;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// Control flow that returns control to the caller.
    /// </summary>
    public sealed class ReturnFlow : BlockFlow
    {
        /// <summary>
        /// Creates return flow that returns a particular value.
        /// </summary>
        /// <param name="returnValue">The value to return.</param>
        public ReturnFlow(ValueTag returnValue)
        {
            this.ReturnValue = returnValue;
        }

        /// <summary>
        /// Gets the value returned by this return flow.
        /// </summary>
        /// <returns>The returned value.</returns>
        public ValueTag ReturnValue { get; private set; }

        /// <inheritdoc/>
        public override IReadOnlyList<ValueTag> Arguments => new ValueTag[] { ReturnValue };

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => EmptyArray<Branch>.Value;

        /// <inheritdoc/>
        public override BlockFlow WithArguments(IReadOnlyList<ValueTag> arguments)
        {
            ContractHelpers.Assert(arguments.Count == 1, "Return flow takes exactly one argument.");
            var newReturnValue = arguments[0];
            if (object.ReferenceEquals(newReturnValue, ReturnValue))
            {
                return this;
            }
            else
            {
                return new ReturnFlow(newReturnValue);
            }
        }

        /// <inheritdoc/>
        public override BlockFlow WithBranches(IReadOnlyList<Branch> branches)
        {
            ContractHelpers.Assert(branches.Count == 0, "Return flow does not take any branches.");
            return this;
        }
    }
}