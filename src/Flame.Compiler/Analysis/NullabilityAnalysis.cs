using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Compiler.Instructions;
using Flame.TypeSystem;

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

        /// <summary>
        /// Tells if a particular instruction always produces a non-<c>null</c> pointer.
        /// </summary>
        /// <param name="instruction">
        /// An instruction to test for nullability.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> will always produce a
        /// non-<c>null</c> pointer; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsNonNull(Instruction instruction)
        {
            if (IsNonNull(instruction.ResultType))
            {
                // Maybe the instruction's result type is intrinsically
                // non-nullable. In that case, we don't even need to look
                // at the instruction itself.
                return true;
            }

            // Okay, so the instruction's type is not nullable. But
            // maybe we can know for sure that the instruction's result
            // is not nullable by looking at the instruction's prototype.
            var proto = instruction.Prototype;
            if (IsNonNull(proto))
            {
                return true;
            }

            // At this point, it turns out that neither the instruction's
            // result type nor its prototype guarantee non-nullability.
            //
            // One last thing that we can try is to do is infer non-nullability
            // from the instruction's arguments for particular prototypes, like
            // reinterpret casts.
            if (proto is ReinterpretCastPrototype
                || proto is CopyPrototype)
            {
                return IsNonNull(instruction.Arguments[0]);
            }

            return false;
        }

        /// <summary>
        /// Tells if a particular instruction always produces a
        /// pointer that is either dereferenceable or <c>null</c>.
        /// </summary>
        /// <param name="instruction">
        /// An instruction to test for dereferenceability.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> may produce a <c>null</c>
        /// pointer; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsDereferenceableOrNull(Instruction instruction)
        {
            if (IsDereferenceableOrNull(instruction.ResultType))
            {
                // Maybe the instruction's result type is intrinsically
                // dereferenceable or null. In that case, we don't even
                // need to look at the instruction itself.
                return true;
            }

            // Okay, so the instruction's type is not necessarily
            // dereferenceable or null. But maybe we can know for
            // sure that the instruction's result is dereferenceable
            // or null by looking at the instruction's prototype.
            var proto = instruction.Prototype;
            if (IsDereferenceableOrNull(proto))
            {
                return true;
            }

            // At this point, it turns out that neither the instruction's
            // result type nor its prototype guarantee dereferenceability.
            //
            // One last thing that we can try is to do is infer dereferenceability
            // from the instruction's arguments for particular prototypes, like
            // copies.
            if (proto is CopyPrototype
                && IsNonNull(instruction.Arguments[0]))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tests if a type is never nullable.
        /// </summary>
        /// <param name="type">
        /// The type to test for non-nullability.
        /// </param>
        /// <returns>
        /// <c>true</c> if a <c>null</c> pointer is an instance of
        /// <paramref name="type"/>; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsNonNull(IType type)
        {
            var pointerType = type as PointerType;
            if (pointerType == null)
            {
                return !(type is IGenericParameter);
            }
            else
            {
                return pointerType.Kind == PointerKind.Reference;
            }
        }

        /// <summary>
        /// Tests if a type is always either dereferenceable or <c>null</c>.
        /// </summary>
        /// <param name="type">
        /// The type to test for dereferenceability.
        /// </param>
        /// <returns>
        /// <c>true</c> all instances of <paramref name="type"/> are either
        /// dereferenceable or <c>null</c>; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsDereferenceableOrNull(IType type)
        {
            var pointerType = type as PointerType;
            if (pointerType == null)
            {
                return !(type is IGenericParameter);
            }
            else
            {
                return pointerType.Kind == PointerKind.Box
                    || pointerType.Kind == PointerKind.Reference;
            }
        }

        internal static bool IsNonNull(InstructionPrototype prototype)
        {
            return IsDereferenceable(prototype);
        }

        internal static bool IsDereferenceableOrNull(InstructionPrototype prototype)
        {
            return IsDereferenceable(prototype);
        }

        private static bool IsDereferenceable(InstructionPrototype prototype)
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
            //
            // * Ditto for get-static-field-pointer instructions.

            return prototype is AllocaPrototype
                || prototype is UnboxPrototype
                || prototype is GetFieldPointerPrototype
                || prototype is GetStaticFieldPointerPrototype
                || prototype is NewObjectPrototype;
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
            foreach (var instruction in graph.NamedInstructions)
            {
                var proto = instruction.Prototype;
                var tag = instruction.Tag;
                if (!ValueNullability.IsNonNull(proto))
                {
                    nullable.Add(tag);
                }

                if (!ValueNullability.IsDereferenceableOrNull(proto))
                {
                    nonderef.Add(tag);
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
