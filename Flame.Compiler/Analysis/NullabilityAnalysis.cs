using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// An analysis result that tells if values produce non-<c>null</c>
    /// or dereferenceable pointers.
    /// </summary>
    public abstract class ValueNullability
    {
        /// <summary>
        /// Tells if a particular value always produces a non-<c>null</c> pointer.
        /// </summary>
        /// <param name="value">
        /// A pointer value to test for nullability.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> will always produce a
        /// non-<c>null</c> pointer; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsNonNull(ValueTag value);

        /// <summary>
        /// Tells if a particular value always produces a pointer that is
        /// either dereferenceable or <c>null</c>.
        /// </summary>
        /// <param name="value">
        /// A pointer value to test for dereferenceability.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> may produce a <c>null</c>
        /// pointer; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool IsDereferenceableOrNull(ValueTag value);

        /// <summary>
        /// Tells if a particular value always produces a
        /// pointer that can be dereferenced.
        /// </summary>
        /// <param name="value">
        /// A pointer value to test for dereferenceability.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> may produce a <c>null</c>
        /// pointer; otherwise, <c>false</c>.
        /// </returns>
        public bool IsDereferenceable(ValueTag value)
        {
            return IsNonNull(value) && IsDereferenceableOrNull(value);
        }
    }

    /// <summary>
    /// A nullability analysis implementation that explicitly
    /// lists all nullable values.
    /// </summary>
    internal sealed class ExplicitValueNullability : ValueNullability
    {
        public ExplicitValueNullability(
            ImmutableHashSet<ValueTag> nullablePointers,
            ImmutableHashSet<ValueTag> nonDereferenceablePointers)
        {
            this.NullablePointers = nullablePointers;
            this.NonDereferenceablePointers = nonDereferenceablePointers;
        }

        /// <summary>
        /// Gets a list of all pointer values that can be <c>null</c>.
        /// </summary>
        /// <value>
        /// A list of values.
        /// </value>
        public ImmutableHashSet<ValueTag> NullablePointers { get; private set; }

        /// <summary>
        /// Gets a list of all pointer values that may not be dereferenceable.
        /// </summary>
        /// <value>
        /// A list of values.
        /// </value>
        public ImmutableHashSet<ValueTag> NonDereferenceablePointers { get; private set; }

        /// <inheritdoc/>
        public override bool IsDereferenceableOrNull(ValueTag value)
        {
            return !NonDereferenceablePointers.Contains(value);
        }

        /// <inheritdoc/>
        public override bool IsNonNull(ValueTag value)
        {
            return !NullablePointers.Contains(value);
        }
    }

    /// <summary>
    /// A simple nullability analysis.
    /// </summary>
    public sealed class NullabilityAnalysis : IFlowGraphAnalysis<ValueNullability>
    {
        private NullabilityAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the nullability analysis.
        /// </summary>
        /// <returns>An instance of the nullability analysis.</returns>
        public static readonly NullabilityAnalysis Instance = new NullabilityAnalysis();

        /// <inheritdoc/>
        public ValueNullability Analyze(FlowGraph graph)
        {
            // TODO: implement a more thorough analysis.
            var nullable = ImmutableHashSet.CreateBuilder<ValueTag>();
            var nonderef = ImmutableHashSet.CreateBuilder<ValueTag>();
            // Assume that all parameters may be nullable and/or non-dereferenceable.
            nullable.UnionWith(graph.ParameterTags);
            nonderef.UnionWith(graph.ParameterTags);
            // Assume that all instructions except for some may be nullable
            // and/or non-dereferenceable.
            foreach (var instruction in graph.Instructions)
            {
                var proto = instruction.Instruction.Prototype;
                if (proto is AllocaPrototype
                    || proto is UnboxPrototype
                    || proto is GetFieldPointerPrototype)
                {
                    // * Allocas always produce non-null pointers that
                    //   are dereferenceable.
                    //
                    // * Unbox instructions that succeed always produce
                    //   non-null pointers that are dereferenceable. If
                    //   they don't succeed, then an exception is thrown
                    //   and the result can't be used.
                    //
                    // * Get-field-pointer instructions that succeed
                    //   always produce non-null pointers that are
                    //   dereferenceable. If they don't succeed, then
                    //   an exception is thrown and the result can't be used.
                    continue;
                }
                else
                {
                    nullable.Add(instruction);
                    nonderef.Add(instruction);   
                }
            }
            return new ExplicitValueNullability(
                nullable.ToImmutable(),
                nonderef.ToImmutable());
        }

        /// <inheritdoc/>
        public ValueNullability AnalyzeWithUpdates(
            FlowGraph graph,
            ValueNullability previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
