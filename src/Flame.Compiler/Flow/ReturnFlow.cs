using System;
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
        public ReturnFlow(Instruction returnValue)
        {
            this.ReturnValue = returnValue;
        }

        /// <summary>
        /// Gets the value returned by this return flow.
        /// </summary>
        /// <returns>The returned value.</returns>
        public Instruction ReturnValue { get; private set; }

        /// <inheritdoc/>
        public override IReadOnlyList<Instruction> Instructions => new Instruction[] { ReturnValue };

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => EmptyArray<Branch>.Value;

        /// <inheritdoc/>
        public override BlockFlow WithInstructions(IReadOnlyList<Instruction> instructions)
        {
            ContractHelpers.Assert(instructions.Count == 1, "Return flow takes exactly one instruction.");
            var newReturnValue = instructions[0];
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

        /// <inheritdoc/>
        public override InstructionBuilder GetInstructionBuilder(
            BasicBlockBuilder block,
            int instructionIndex)
        {
            if (instructionIndex == 0)
            {
                return new SimpleFlowInstructionBuilder(block);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
}
