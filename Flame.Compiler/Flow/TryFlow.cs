using System;
using System.Collections.Generic;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// Control flow that executes an instruction and checks if that
    /// instruction throws. If it does, an exception-path branch is
    /// taken. Otherwise, a success-path branch is taken.
    /// </summary>
    public sealed class TryFlow : BlockFlow
    {
        /// <summary>
        /// Creates 'try' flow from an instruction, a branch to take
        /// if that instruction does not throw and a branch to take
        /// if the instruction does throw.
        /// </summary>
        /// <param name="instruction">
        /// The inner instruction to execute.
        /// </param>
        /// <param name="successBranch">
        /// The branch to take if the instruction does not throw.
        /// </param>
        /// <param name="exceptionBranch">
        /// The branch to take if the instruction throws.
        /// </param>
        public TryFlow(
            Instruction instruction,
            Branch successBranch,
            Branch exceptionBranch)
        {
            this.Instruction = instruction;
            this.SuccessBranch = successBranch;
            this.ExceptionBranch = exceptionBranch;
        }

        /// <summary>
        /// Gets the instruction this 'try' flow tries to execute.
        /// </summary>
        /// <returns>The inner instruction.</returns>
        public Instruction Instruction { get; private set; }

        /// <summary>
        /// Gets the branch this 'try' flow takes if the
        /// instruction does not throw.
        /// </summary>
        /// <returns>The success branch.</returns>
        public Branch SuccessBranch { get; private set; }

        /// <summary>
        /// Gets the branch this 'try' flow takes if the
        /// instruction throws.
        /// </summary>
        /// <returns>The exception branch.</returns>
        public Branch ExceptionBranch { get; private set; }

        /// <inheritdoc/>
        public override IReadOnlyList<Instruction> Instructions => new Instruction[] { Instruction };

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => new Branch[] { SuccessBranch, ExceptionBranch };

        /// <inheritdoc/>
        public override InstructionBuilder GetInstructionBuilder(BasicBlockBuilder block, int instructionIndex)
        {
            if (instructionIndex == 0)
            {
                return new TryFlowInstructionBuilder(block);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        public override BlockFlow WithBranches(IReadOnlyList<Branch> branches)
        {
            ContractHelpers.Assert(branches.Count == 2, "Try flow takes exactly two branches.");
            var success = branches[0];
            var exn = branches[1];
            if (success == SuccessBranch && exn == ExceptionBranch)
            {
                return this;
            }
            else
            {
                return new TryFlow(Instruction, success, exn);
            }
        }

        /// <inheritdoc/>
        public override BlockFlow WithInstructions(IReadOnlyList<Instruction> instructions)
        {
            ContractHelpers.Assert(instructions.Count == 1, "Try flow takes exactly one instruction.");
            var insn = instructions[0];
            if (insn == Instruction)
            {
                return this;
            }
            else
            {
                return new TryFlow(insn, SuccessBranch, ExceptionBranch);
            }
        }
    }
}
