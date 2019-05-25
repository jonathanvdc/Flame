using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// An allocation of values to registers.
    /// </summary>
    /// <typeparam name="TRegister">
    /// The type of register allocated to values.
    /// </typeparam>
    public struct RegisterAllocation<TRegister>
    {
        /// <summary>
        /// Creates a register allocation container from a value-to-register map.
        /// </summary>
        /// <param name="allocation"></param>
        public RegisterAllocation(
            IReadOnlyDictionary<ValueTag, TRegister> allocation)
        {
            this.Allocation = allocation;
        }

        /// <summary>
        /// Gets a mapping of values to the registers they
        /// are allocated to.
        /// </summary>
        /// <value>A mapping of values to registers.</value>
        public IReadOnlyDictionary<ValueTag, TRegister> Allocation { get; private set; }

        /// <summary>
        /// Gets the register allocated to a particular value.
        /// </summary>
        /// <param name="value">
        /// The value to find a register for.
        /// </param>
        /// <returns>
        /// A register.
        /// </returns>
        public TRegister GetRegister(ValueTag value)
        {
            return Allocation[value];
        }
    }

    /// <summary>
    /// An analysis that greedily allocates registers to values.
    /// The set of values is assumed to be unbounded: the analysis
    /// is allowed to "create" an arbitrarily large amount of
    /// registers.
    /// </summary>
    /// <typeparam name="TRegister">
    /// The type of register allocated to values.
    /// </typeparam>
    public abstract class GreedyRegisterAllocator<TRegister> : IFlowGraphAnalysis<RegisterAllocation<TRegister>>
    {
        /// <summary>
        /// Creates a brand new register for a value of a particular type.
        /// </summary>
        /// <param name="type">
        /// The type of value to create a register for.
        /// </param>
        /// <returns>A register suitable for the value.</returns>
        protected abstract TRegister CreateRegister(IType type);

        /// <summary>
        /// Tries to recycle a register from a set of registers.
        /// </summary>
        /// <param name="type">
        /// The type of value to store in the recycled register.
        /// </param>
        /// <param name="registers">
        /// A set of registers that are eligible for recycling.
        /// </param>
        /// <param name="result">
        /// A register to recycle, if any.
        /// </param>
        /// <returns>
        /// <c>true</c> if a register has been selected for recyling;
        /// otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool TryRecycleRegister(
            IType type,
            IEnumerable<TRegister> registers,
            out TRegister result);

        /// <summary>
        /// Tells if a register should be allocated for a
        /// particular value.
        /// </summary>
        /// <param name="value">
        /// The value for which register allocation may or may
        /// not be necessary.
        /// </param>
        /// <param name="graph">
        /// The control flow graph that defines <paramref name="value"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if a register must be allocated to
        /// <paramref name="value"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Implementations may override this method to suppress
        /// register allocation for values that are, e.g., stored
        /// on an evaluation stack.
        /// </remarks>
        protected virtual bool RequiresRegister(
            ValueTag value,
            FlowGraph graph)
        {
            return true;
        }

        /// <summary>
        /// Tries to get a preallocated register for a particular value.
        /// If it exists, then the preallocated register will be used
        /// for the value, no questions asked. The preallocated register
        /// may be reused.
        /// </summary>
        /// <param name="value">
        /// The value that may have a preallocated register.
        /// </param>
        /// <param name="graph">
        /// The graph that defines the value.
        /// </param>
        /// <param name="register">
        /// A preallocated register, if any.
        /// </param>
        /// <returns>
        /// <c>true</c> if there is a preallocated register for
        /// <paramref name="value"/>; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool TryGetPreallocatedRegister(
            ValueTag value,
            FlowGraph graph,
            out TRegister register)
        {
            register = default(TRegister);
            return false;
        }

        /// <inheritdoc/>
        public RegisterAllocation<TRegister> Analyze(
            FlowGraph graph)
        {
            // Run the related values and interference graph analyses.
            var related = graph.GetAnalysisResult<RelatedValues>();
            var interference = graph.GetAnalysisResult<InterferenceGraph>();

            // Create a mapping of values to registers. This will become our
            // return value.
            var allocation = new Dictionary<ValueTag, TRegister>();

            // Create a mapping of registers to the set of all values they
            // interfere with due to the registers getting allocated to values.
            var registerInterference = new Dictionary<TRegister, HashSet<ValueTag>>();

            // Before we do any real register allocation, we should set up
            // preallocated registers. The reason for doing this now instead
            // of later on in the allocation loop is that preallocated registers
            // are "free:" they don't incur register allocations. At the same time,
            // preallocated registers can be recycled, so we'll have more
            // registers to recycle (and hopefully create fewer new ones) if we
            // handle preallocated registers first.
            foreach (var value in graph.ValueTags)
            {
                TRegister assignedRegister;
                if (TryGetPreallocatedRegister(value, graph, out assignedRegister))
                {
                    // If we have a preallocated register, then we should just accept it.
                    allocation[value] = assignedRegister;
                    registerInterference[assignedRegister] = new HashSet<ValueTag>(
                        interference.GetInterferingValues(value));
                }
            }

            // Iterate over all values in the graph.
            foreach (var value in graph.ValueTags)
            {
                if (!RequiresRegister(value, graph) || allocation.ContainsKey(value))
                {
                    // The value may not need a register or may already have one.
                    // If so, then we shouldn't allocate one.
                    continue;
                }

                // Compose a set of registers we might be able to recycle.
                // Specifically, we'll look for all registers that are not
                // allocated to values that interfere with the current value.
                var recyclable = new HashSet<TRegister>();
                foreach (var pair in registerInterference)
                {
                    if (!pair.Value.Contains(value))
                    {
                        // If the value is not in the interference set of
                        // the register, then we're good to go.
                        recyclable.Add(pair.Key);
                    }
                }

                // We would like to recycle a register that has been
                // allocated to a related but non-interfering value.
                // To do so, we'll build a set of candidate registers.
                var relatedRegisters = new HashSet<TRegister>();
                foreach (var relatedValue in related.GetRelatedValues(value))
                {
                    TRegister reg;
                    if (allocation.TryGetValue(relatedValue, out reg)
                        && recyclable.Contains(reg))
                    {
                        relatedRegisters.Add(reg);
                    }
                }

                // If at all possible, try to recycle a related register. If that
                // doesn't work out, try to recycle a non-related register. If
                // that fails as well, then we'll create a new register.
                var valueType = graph.GetValueType(value);
                TRegister assignedRegister;
                if (!TryRecycleRegister(valueType, relatedRegisters, out assignedRegister)
                    && !TryRecycleRegister(valueType, recyclable, out assignedRegister))
                {
                    assignedRegister = CreateRegister(valueType);
                    registerInterference[assignedRegister] = new HashSet<ValueTag>();
                }

                // Allocate the register we recycled or created to the value.
                allocation[value] = assignedRegister;
                registerInterference[assignedRegister].UnionWith(
                    interference.GetInterferingValues(value));
            }

            return new RegisterAllocation<TRegister>(allocation);
        }

        /// <inheritdoc/>
        public RegisterAllocation<TRegister> AnalyzeWithUpdates(
            FlowGraph graph,
            RegisterAllocation<TRegister> previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
