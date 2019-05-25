using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A mapping of instructions to memory SSA states.
    /// </summary>
    public sealed class MemorySSA
    {
        /// <summary>
        /// Creates a memory SSA mapping.
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
        public Value GetMemoryAfter(NamedInstruction instruction)
        {
            return InstructionValues[instruction];
        }

        /// <summary>
        /// Gets the memory state before a particular instruction has executed.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>A memory state.</returns>
        public Value GetMemoryBefore(NamedInstruction instruction)
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
        public Value GetMemoryAfter(NamedInstructionBuilder instruction)
        {
            return GetMemoryAfter(instruction.ToImmutable());
        }

        /// <summary>
        /// Gets the memory state before a particular instruction has executed.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>A memory state.</returns>
        public Value GetMemoryBefore(NamedInstructionBuilder instruction)
        {
            return GetMemoryBefore(instruction.ToImmutable());
        }

        /// <summary>
        /// A memory SSA value, i.e., a state that memory can have.
        /// </summary>
        public abstract class Value
        {
            /// <summary>
            /// Tries to ascertain the value stored at a particular
            /// address.
            /// </summary>
            /// <param name="address">
            /// The address to examine.
            /// </param>
            /// <param name="graph">
            /// The graph that defines the address value.
            /// </param>
            /// <param name="value">
            /// This method's result: the value that is stored at the address, if any.
            /// </param>
            /// <returns>
            /// <c>true</c> if the value at <paramref name="address"/>; otherwise, <c>false</c>.
            /// </returns>
            public bool TryGetValueAt(
                ValueTag address,
                FlowGraph graph,
                out ValueTag value)
            {
                if (this is Store)
                {
                    var storeState = (Store)this;

                    // Try a cheap check before we move on to alias analysis.
                    if (address == storeState.Address)
                    {
                        value = storeState.Value;
                        return true;
                    }

                    var aliasing = graph.GetAnalysisResult<AliasAnalysisResult>();
                    var aliasStatus = aliasing.GetAliasing(address, storeState.Value);
                    if (aliasStatus == Aliasing.MustAlias)
                    {
                        // Must-alias means we discovered the value at the address.
                        value = storeState.Value;
                        return true;
                    }
                    else if (aliasStatus == Aliasing.NoAlias)
                    {
                        // No-alias mean we can just try the previous state.
                        return storeState.Operand.TryGetValueAt(address, graph, out value);
                    }
                    else
                    {
                        // May-alias means we can't know for sure what the value at
                        // the address is, so we'll bail.
                        value = null;
                        return false;
                    }
                }
                else
                {
                    // The state is either unknown or a phi.
                    // TODO: handle phis properly.
                    value = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// A memory SSA value that represents a completely unknown state.
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
        /// A memory SSA value that represents an update of another memory SSA
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
            /// <param name="address">
            /// The address that is written to.
            /// </param>
            /// <param name="value">
            /// The value that is written to the address.
            /// </param>
            public Store(Value operand, ValueTag address, ValueTag value)
            {
                this.Operand = operand;
                this.Address = address;
                this.Value = value;
            }

            /// <summary>
            /// Gets the memory SSA value that is updated.
            /// </summary>
            /// <value>A memory state.</value>
            public Value Operand { get; private set; }

            /// <summary>
            /// Gets the address that is written to.
            /// </summary>
            /// <value>An address.</value>
            public ValueTag Address { get; private set; }

            /// <summary>
            /// Gets the value that is stored at that address.
            /// </summary>
            /// <value>A value.</value>
            public ValueTag Value { get; private set; }

            /// <summary>
            /// Creates a memory SSA state that represents an existing state,
            /// updated by a store to a particular address. If the store is
            /// a no-op, then the current state is returned.
            /// </summary>
            /// <param name="state">
            /// The state that gets updated.
            /// </param>
            /// <param name="address">
            /// The address to which <paramref name="value"/> value is written.
            /// </param>
            /// <param name="value">
            /// A value that is written to an address.
            /// </param>
            /// <param name="graph">
            /// The control-flow graph that performs the update.
            /// </param>
            /// <returns>
            /// A memory SSA state.
            /// </returns>
            public static Value WithStore(
                Value state,
                ValueTag address,
                ValueTag value,
                FlowGraph graph)
            {
                ValueTag prevValue;
                if (state.TryGetValueAt(address, graph, out prevValue))
                {
                    var numbering = graph.GetAnalysisResult<ValueNumbering>();
                    if (numbering.AreEquivalent(address, value))
                    {
                        // The store is a nop if the old value and the new value
                        // at the address are the same.
                        return state;
                    }
                }
                return new Store(state, address, value);
            }
        }

        /// <summary>
        /// A memory SSA phi, which sets the memory state to one of a list of
        /// potential memory SSA states.
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

    /// <summary>
    /// A very simple, block-local memory SSA analysis.
    /// </summary>
    public sealed class LocalMemorySSAAnalysis : IFlowGraphAnalysis<MemorySSA>
    {
        private LocalMemorySSAAnalysis()
        { }

        /// <summary>
        /// An instance of the local memory SSA analysis.
        /// </summary>
        public static readonly LocalMemorySSAAnalysis Instance =
            new LocalMemorySSAAnalysis();

        /// <inheritdoc/>
        public MemorySSA Analyze(FlowGraph graph)
        {
            // Set the initial memory SSA state to 'unknown' at the start of every
            // basic block.
            var blockStates = ImmutableDictionary<BasicBlockTag, MemorySSA.Value>.Empty.ToBuilder();
            foreach (var block in graph.BasicBlockTags)
            {
                blockStates[block] = MemorySSA.Unknown.Instance;
            }

            // Step through the instructions at every block, updating the
            // memory SSA state as we go.
            var insnStates = ImmutableDictionary<ValueTag, MemorySSA.Value>.Empty.ToBuilder();
            foreach (var block in graph.BasicBlocks)
            {
                var state = blockStates[block];
                foreach (var instruction in block.NamedInstructions)
                {
                    state = UpdateState(state, instruction);
                    insnStates[instruction] = state;
                }
            }

            return new MemorySSA(blockStates.ToImmutable(), insnStates.ToImmutable());
        }

        private static MemorySSA.Value UpdateState(MemorySSA.Value state, NamedInstruction instruction)
        {
            var graph = instruction.Block.Graph;

            var proto = instruction.Prototype;
            if (proto is StorePrototype)
            {
                // Stores are special. They map directly to a memory SSA store.
                var storeProto = (StorePrototype)proto;
                var pointer = storeProto.GetPointer(instruction.Instruction);
                var value = storeProto.GetValue(instruction.Instruction);
                return MemorySSA.Store.WithStore(state, pointer, value, graph);
            }
            else if (proto is LoadPrototype)
            {
                return state;
            }
            else if (graph.GetAnalysisResult<EffectfulInstructions>().Instructions.Contains(instruction))
            {
                // Instructions that affect memory in unpredictable ways produce
                // an unknown state.
                //
                // TODO: reduce the number of instructions that produce an unknown state.
                // At the moment, all effectful instructions are considered to produce
                // an unknown memory state. However, many instructions that may throw
                // don't affect the (accessible) memory state at all. We need some way
                // to query whether instructions may write to memory or not.
                return MemorySSA.Unknown.Instance;
            }
            else
            {
                return state;
            }
        }

        /// <inheritdoc/>
        public MemorySSA AnalyzeWithUpdates(
            FlowGraph graph,
            MemorySSA previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            // Just re-analyze.
            return Analyze(graph);
        }
    }
}
