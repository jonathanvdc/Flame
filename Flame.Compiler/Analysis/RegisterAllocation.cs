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

            // Iterate over all values in the graph.
            foreach (var value in graph.ValueTags)
            {
                if (!RequiresRegister(value, graph))
                {
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
                TRegister recycledReg;
                if (!TryRecycleRegister(valueType, relatedRegisters, out recycledReg)
                    && !TryRecycleRegister(valueType, recyclable, out recycledReg))
                {
                    recycledReg = CreateRegister(valueType);
                    registerInterference[recycledReg] = new HashSet<ValueTag>();
                }

                // Allocate the register we recycled or created to the value.
                allocation[value] = recycledReg;
                registerInterference[recycledReg].UnionWith(
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
