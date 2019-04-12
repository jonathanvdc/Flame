using System;
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
        public override IReadOnlyList<Instruction> Instructions => EmptyArray<Instruction>.Value;

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => EmptyArray<Branch>.Value;

        /// <inheritdoc/>
        public override BlockFlow WithInstructions(IReadOnlyList<Instruction> instructions)
        {
            ContractHelpers.Assert(instructions.Count == 0, "Unreachable flow does not take any instructions.");
            return this;
        }

        /// <inheritdoc/>
        public override BlockFlow WithBranches(IReadOnlyList<Branch> branches)
        {
            ContractHelpers.Assert(branches.Count == 0, "Unreachable flow does not take any branches.");
            return this;
        }

        /// <inheritdoc/>
        public override InstructionBuilder GetInstructionBuilder(BasicBlockBuilder block, int instructionIndex)
        {
            throw new IndexOutOfRangeException();
        }
    }
}