using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A mapping of instructions to memory-SSA states.
    /// </summary>
    public sealed class MemorySSA
    {
        /// <summary>
        /// Creates a memory-SSA mapping.
        /// </summary>
        /// <param name="blockValues">
        /// A mapping of basic block entry points to memory states.
        /// </param>
        /// <param name="instructionValues">
        /// A mapping of instructions to memory states.
        /// </param>
        public MemorySSA(
            ImmutableDictionary<BasicBlockTag, Value> blockValues,
            ImmutableDictionary<ValueTag, Value> instructionValues)
        {
            this.BlockValues = blockValues;
            this.InstructionValues = instructionValues;
        }

        /// <summary>
        /// Gets the mapping of basic block entry points to memory states.
        /// </summary>
        /// <value>An immutable dictionary.</value>
        public ImmutableDictionary<BasicBlockTag, Value> BlockValues { get; private set; }

        /// <summary>
        /// Gets the mapping of instructions to memory states.
        /// </summary>
        /// <value>An immutable dictionary.</value>
        public ImmutableDictionary<ValueTag, Value> InstructionValues { get; private set; }

        /// <summary>
        /// Gets the memory state at the start of a particular basic block.
        /// </summary>
        /// <param name="block">A basic block.</param>
        /// <returns>A memory state.</returns>
        public Value GetMemoryAtEntry(BasicBlockTag block)
        {
            return BlockValues[block];
        }

        /// <summary>
        /// Gets the memory state after a particular instruction has executed.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>A memory state.</returns>
        public Value GetMemoryAfter(SelectedInstruction instruction)
        {
            return InstructionValues[instruction];
        }

        /// <summary>
        /// Gets the memory state before a particular instruction has executed.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>A memory state.</returns>
        public Value GetMemoryBefore(SelectedInstruction instruction)
        {
            var prev = instruction.PreviousInstructionOrNull;
            if (prev == null)
            {
                return GetMemoryAtEntry(instruction.Block);
            }
            else
            {
                return GetMemoryAfter(prev);
            }
        }

        /// <summary>
        /// Gets the memory state after a particular instruction has executed.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>A memory state.</returns>
        public Value GetMemoryAfter(InstructionBuilder instruction)
        {
            return GetMemoryAfter(instruction.ToImmutable());
        }

        /// <summary>
        /// Gets the memory state before a particular instruction has executed.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>A memory state.</returns>
        public Value GetMemoryBefore(InstructionBuilder instruction)
        {
            return GetMemoryBefore(instruction.ToImmutable());
        }

        /// <summary>
        /// A memory-SSA value, i.e., a state that memory can have.
        /// </summary>
        public abstract class Value
        { }

        /// <summary>
        /// A memory-SSA value that represents a completely unknown state.
        /// </summary>
        public sealed class Unknown : Value
        {
            private Unknown()
            { }

            /// <summary>
            /// An instance of the unknown state.
            /// </summary>
            /// <returns>The unknown state.</returns>
            public static readonly Unknown Instance = new Unknown();
        }

        /// <summary>
        /// A memory-SSA value that represents an update of another memory-SSA
        /// value.
        /// </summary>
        public sealed class Store : Value
        {
            /// <summary>
            /// Creates a store.
            /// </summary>
            /// <param name="operand">
            /// The memory state to update.
            /// </param>
            public Store(Value operand)
            {
                this.Operand = operand;
            }

            /// <summary>
            /// The memory-SSA value that is updated.
            /// </summary>
            /// <value>A memory state.</value>
            public Value Operand { get; private set; }
        }

        /// <summary>
        /// A memory-SSA phi, which sets the memory state to one of a list of
        /// potential memory-SSA states.
        /// </summary>
        public sealed class Phi : Value
        {
            /// <summary>
            /// Creates a phi.
            /// </summary>
            /// <param name="operands">
            /// The phi's operands.
            /// </param>
            public Phi(IReadOnlyList<Value> operands)
            {
                this.Operands = operands;
            }

            /// <summary>
            /// Gets the phi's operands.
            /// </summary>
            /// <value>The phi's operands.</value>
            public IReadOnlyList<Value> Operands { get; private set; }
        }
    }
}
