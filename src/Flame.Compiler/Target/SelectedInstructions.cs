using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;

namespace Flame.Compiler.Target
{
    /// <summary>
    /// A collection of selected instructions for a value.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of a target instruction.
    /// </typeparam>
    public struct SelectedInstructions<TInstruction>
    {
        /// <summary>
        /// Creates a selected instruction container.
        /// </summary>
        /// <param name="instructions">
        /// The instructions selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public SelectedInstructions(
            IReadOnlyList<TInstruction> instructions,
            IReadOnlyList<ValueTag> dependencies)
        {
            this.Instructions = instructions;
            this.Dependencies = dependencies;
        }

        /// <summary>
        /// Creates a selected instruction container from a single
        /// instruction and a variable number of dependencies.
        /// </summary>
        /// <param name="instruction">
        /// The instruction selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public SelectedInstructions(
            TInstruction instruction,
            params ValueTag[] dependencies)
            : this(new[] { instruction }, dependencies)
        { }

        /// <summary>
        /// Gets the list of instructions in this container.
        /// </summary>
        /// <value>A list of instructions.</value>
        public IReadOnlyList<TInstruction> Instructions { get; private set; }

        /// <summary>
        /// Gets the list of values these selected instructions depend on.
        /// </summary>
        /// <value>A list of values.</value>
        public IReadOnlyList<ValueTag> Dependencies { get; private set; }

        /// <summary>
        /// Prepends a sequence of instructions to these selected instructions,
        /// returning the new selected instructions as a new object.
        /// </summary>
        /// <param name="prefix">The selected instructions to prepend.</param>
        /// <returns>
        /// A collection of selected instructions that is identical to these, but
        /// with <paramref name="prefix"/> prepended to the instructions.
        /// </returns>
        public SelectedInstructions<TInstruction> Prepend(IReadOnlyList<TInstruction> prefix)
        {
            return new SelectedInstructions<TInstruction>(
                prefix.Concat(Instructions).ToArray(),
                Dependencies);
        }
    }

    /// <summary>
    /// A collection of selected instructions for a value.
    /// </summary>
    public static class SelectedInstructions
    {
        /// <summary>
        /// Creates a selected instruction container.
        /// </summary>
        /// <param name="instructions">
        /// The instructions selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public static SelectedInstructions<TInstruction> Create<TInstruction>(
            IReadOnlyList<TInstruction> instructions,
            IReadOnlyList<ValueTag> dependencies)
        {
            return new SelectedInstructions<TInstruction>(instructions, dependencies);
        }

        /// <summary>
        /// Creates a selected instruction container from a single
        /// instruction and a variable number of dependencies.
        /// </summary>
        /// <param name="instruction">
        /// The instruction selected for a particular value.
        /// </param>
        /// <param name="dependencies">
        /// The list of values the selected instructions
        /// depend on.
        /// </param>
        public static SelectedInstructions<TInstruction> Create<TInstruction>(
            TInstruction instruction,
            params ValueTag[] dependencies)
        {
            return new SelectedInstructions<TInstruction>(instruction, dependencies);
        }

        /// <summary>
        /// Creates a selected instruction container from a sequence of instructions
        /// and no dependencies.
        /// </summary>
        /// <param name="instructions">
        /// A sequence of instructions as selected for a particular value.
        /// </param>
        public static SelectedInstructions<TInstruction> Create<TInstruction>(
            IReadOnlyList<TInstruction> instructions)
        {
            return new SelectedInstructions<TInstruction>(instructions, EmptyArray<ValueTag>.Value);
        }
    }

    /// <summary>
    /// A collection of selected instructions for block flow.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of a target instruction.
    /// </typeparam>
    public struct SelectedFlowInstructions<TInstruction>
    {
        /// <summary>
        /// Creates an instruction selection for block flow.
        /// </summary>
        /// <param name="chunks">
        /// A sequence of instruction selection chunks.
        /// Every chunk represents an logical instruction that loads its
        /// dependencies in order and performs some action.
        /// </param>
        public SelectedFlowInstructions(
            IReadOnlyList<SelectedInstructions<TInstruction>> chunks)
        {
            this.Chunks = chunks;
        }

        /// <summary>
        /// Gets the sequence of instruction selection chunks that constitutes this
        /// instruction selection.
        /// </summary>
        /// <value>
        /// A sequence of instruction selection chunks.
        /// Every chunk represents an logical instruction that loads its
        /// dependencies in order and performs some action.
        /// </value>
        public IReadOnlyList<SelectedInstructions<TInstruction>> Chunks { get; private set; }

        /// <summary>
        /// Gets the sequence of values these selected instructions depend on.
        /// </summary>
        /// <value>A sequence of values.</value>
        public IEnumerable<ValueTag> Dependencies => Chunks.SelectMany(c => c.Dependencies);

        /// <summary>
        /// Appends a sequence of instructions to these selected flow instructions,
        /// producing a new flow instruction selection.
        /// </summary>
        /// <param name="extraInstructions">Additional instructions to append.</param>
        /// <returns>A flow instruction selection.</returns>
        public SelectedFlowInstructions<TInstruction> Append(
            IEnumerable<TInstruction> extraInstructions)
        {
            return new SelectedFlowInstructions<TInstruction>(
                Chunks.Concat(
                    new[]
                    {
                        new SelectedInstructions<TInstruction>(
                            extraInstructions.ToArray(),
                            EmptyArray<ValueTag>.Value)
                    })
                    .ToArray());
        }

        /// <summary>
        /// Appends a sequence of instructions to these selected flow instructions,
        /// producing a new flow instruction selection.
        /// </summary>
        /// <param name="extraInstructions">Additional instructions to append.</param>
        /// <returns>A flow instruction selection.</returns>
        public SelectedFlowInstructions<TInstruction> Append(
            params TInstruction[] extraInstructions)
        {
            return Append((IEnumerable<TInstruction>)extraInstructions);
        }

        /// <summary>
        /// Prepends a sequence of instructions to these selected flow instructions,
        /// producing a new flow instruction selection.
        /// </summary>
        /// <param name="extraInstructions">Additional instructions to prepend.</param>
        /// <returns>A flow instruction selection.</returns>
        public SelectedFlowInstructions<TInstruction> Prepend(
            IEnumerable<TInstruction> extraInstructions)
        {
            return new SelectedFlowInstructions<TInstruction>(
                new[]
                {
                    new SelectedInstructions<TInstruction>(
                        extraInstructions.ToArray(),
                        EmptyArray<ValueTag>.Value)
                }
                .Concat(Chunks)
                .ToArray());
        }

        /// <summary>
        /// Prepends a sequence of instructions to these selected flow instructions,
        /// producing a new flow instruction selection.
        /// </summary>
        /// <param name="extraInstructions">Additional instructions to prepend.</param>
        /// <returns>A flow instruction selection.</returns>
        public SelectedFlowInstructions<TInstruction> Prepend(
            params TInstruction[] extraInstructions)
        {
            return Prepend((IEnumerable<TInstruction>)extraInstructions);
        }
    }

    /// <summary>
    /// A collection of selected instructions for block flow.
    /// </summary>
    public static class SelectedFlowInstructions
    {
        /// <summary>
        /// Creates a flow instruction selection from a sequence of logical instructions.
        /// </summary>
        /// <param name="chunks">
        /// A sequence of logical instructions.
        /// </param>
        /// <typeparam name="TInstruction">
        /// The type of a target instruction.
        /// </typeparam>
        /// <returns>A flow instruction selection.</returns>
        public static SelectedFlowInstructions<TInstruction> Create<TInstruction>(
            IReadOnlyList<SelectedInstructions<TInstruction>> chunks)
        {
            return new SelectedFlowInstructions<TInstruction>(chunks);
        }

        /// <summary>
        /// Creates a flow instruction selection from a sequence of logical instructions.
        /// </summary>
        /// <param name="chunks">
        /// A sequence of logical instructions.
        /// </param>
        /// <typeparam name="TInstruction">
        /// The type of a target instruction.
        /// </typeparam>
        /// <returns>A flow instruction selection.</returns>
        public static SelectedFlowInstructions<TInstruction> Create<TInstruction>(
            params SelectedInstructions<TInstruction>[] chunks)
        {
            return new SelectedFlowInstructions<TInstruction>(chunks);
        }

        /// <summary>
        /// Creates a flow instruction selection from a sequence of target instructions.
        /// </summary>
        /// <param name="instructions">A sequence of instructions to wrap.</param>
        /// <typeparam name="TInstruction">
        /// The type of a target instruction.
        /// </typeparam>
        /// <returns>A flow instruction selection.</returns>
        public static SelectedFlowInstructions<TInstruction> Create<TInstruction>(
            params TInstruction[] instructions)
        {
            return Create(
                SelectedInstructions.Create<TInstruction>(
                    instructions,
                    EmptyArray<ValueTag>.Value));
        }
    }
}
