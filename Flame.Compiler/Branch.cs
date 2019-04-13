using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;

namespace Flame.Compiler
{
    /// <summary>
    /// A branch to a particular block that passes a list
    /// of values as arguments.
    /// </summary>
    public sealed class Branch : IEquatable<Branch>
    {
        /// <summary>
        /// Creates a branch that targets a particular block and
        /// passes no arguments.
        /// </summary>
        /// <param name="target">The target block.</param>
        public Branch(BasicBlockTag target)
            : this(target, EmptyArray<BranchArgument>.Value)
        { }

        /// <summary>
        /// Creates a branch that targets a particular block and
        /// passes a list of arguments.
        /// </summary>
        /// <param name="target">The target block.</param>
        /// <param name="arguments">
        /// A list of arguments to pass to the target block.
        /// </param>
        public Branch(BasicBlockTag target, IReadOnlyList<ValueTag> arguments)
            : this(target, arguments.EagerSelect(BranchArgument.FromValue))
        { }

        /// <summary>
        /// Creates a branch that targets a particular block and
        /// passes a list of arguments.
        /// </summary>
        /// <param name="target">The target block.</param>
        /// <param name="arguments">
        /// A list of arguments to pass to the target block.
        /// </param>
        public Branch(BasicBlockTag target, IReadOnlyList<BranchArgument> arguments)
        {
            this.Target = target;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets the branch's target block.
        /// </summary>
        /// <returns>The target block.</returns>
        public BasicBlockTag Target { get; private set; }

        /// <summary>
        /// Gets the arguments passed to the target block
        /// when this branch is taken.
        /// </summary>
        /// <returns>A list of arguments.</returns>
        public IReadOnlyList<BranchArgument> Arguments { get; private set; }

        /// <summary>
        /// Replaces this branch's target with another block.
        /// </summary>
        /// <param name="target">The new target block.</param>
        /// <returns>A new branch.</returns>
        public Branch WithTarget(BasicBlockTag target)
        {
            return new Branch(target, Arguments);
        }

        /// <summary>
        /// Replaces this branch's arguments with a particular
        /// list of arguments.
        /// </summary>
        /// <param name="arguments">The new arguments.</param>
        /// <returns>A new branch.</returns>
        public Branch WithArguments(IReadOnlyList<BranchArgument> arguments)
        {
            return new Branch(Target, arguments);
        }

        /// <summary>
        /// Creates a branch that is the result of appending
        /// an argument at the end of this branch's argument list.
        /// </summary>
        /// <param name="argument">The argument to add.</param>
        /// <returns>A new branch.</returns>
        public Branch AddArgument(BranchArgument argument)
        {
            var oldArgCount = Arguments.Count;
            var newArgs = new BranchArgument[oldArgCount + 1];
            for (int i = 0; i < oldArgCount; i++)
            {
                newArgs[i] = Arguments[i];
            }
            newArgs[oldArgCount] = argument;
            return WithArguments(newArgs);
        }

        /// <summary>
        /// Creates a branch that is the result of appending
        /// an argument at the end of this branch's argument list.
        /// </summary>
        /// <param name="argument">The argument to add.</param>
        /// <returns>A new branch.</returns>
        public Branch AddArgument(ValueTag argument)
        {
            return AddArgument(BranchArgument.FromValue(argument));
        }

        /// <summary>
        /// Zips this branch's arguments with their corresponding
        /// parameters.
        /// </summary>
        /// <param name="graph">
        /// The graph that defines the branch.
        /// </param>
        /// <returns>
        /// A sequence of key-value pairs where the keys are basic
        /// block parameters and the values are branch arguments.
        /// </returns>
        public IEnumerable<KeyValuePair<ValueTag, BranchArgument>> ZipArgumentsWithParameters(
            FlowGraph graph)
        {
            var targetParams = graph.GetBasicBlock(Target).ParameterTags;
            return targetParams.Zip(
                Arguments,
                (x, y) => new KeyValuePair<ValueTag, BranchArgument>(x, y));
        }

        /// <summary>
        /// Zips this branch's arguments with their corresponding
        /// parameters.
        /// </summary>
        /// <param name="graph">
        /// The graph that defines the branch.
        /// </param>
        /// <returns>
        /// A sequence of key-value pairs where the keys are basic
        /// block parameters and the values are branch arguments.
        /// </returns>
        public IEnumerable<KeyValuePair<ValueTag, BranchArgument>> ZipArgumentsWithParameters(
            FlowGraphBuilder graph)
        {
            return ZipArgumentsWithParameters(graph.ImmutableGraph);
        }

        /// <summary>
        /// Creates a new branch by applying a mapping to every argument in
        /// this branch's argument list.
        /// </summary>
        /// <param name="mapping">
        /// The mapping to apply to every argument in this branch's
        /// argument list.
        /// </param>
        /// <returns>A new branch.</returns>
        public Branch MapArguments(Func<BranchArgument, BranchArgument> mapping)
        {
            return WithArguments(Arguments.EagerSelect(mapping));
        }

        /// <summary>
        /// Creates a new branch by applying a mapping to every value in
        /// this branch's argument list.
        /// </summary>
        /// <param name="mapping">
        /// The mapping to apply to every value in this branch's
        /// argument list.
        /// </param>
        /// <returns>A new branch.</returns>
        public Branch MapArguments(Func<ValueTag, ValueTag> mapping)
        {
            return MapArguments(
                arg =>
                    arg.IsValue
                    ? BranchArgument.FromValue(mapping(arg.ValueOrNull))
                    : arg);
        }

        /// <summary>
        /// Tests if this branch equals another branch.
        /// </summary>
        /// <param name="other">The branch to compare with.</param>
        /// <returns>
        /// <c>true</c> if this branch equals the other branch; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Branch other)
        {
            return !ReferenceEquals(other, null)
                && Target == other.Target
                && Arguments.SequenceEqual(other.Arguments);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Branch && Equals((Branch)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = EnumerableComparer.HashEnumerable(Arguments);
            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                Target.GetHashCode());
            return hashCode;
        }

        /// <summary>
        /// Tests if two branches are equal.
        /// </summary>
        /// <param name="left">
        /// The first branch to compare.
        /// </param>
        /// <param name="right">
        /// The second branch to compare.
        /// </param>
        /// <returns>
        /// <c>true</c> if the branches are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator==(Branch left, Branch right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two branches are not equal.
        /// </summary>
        /// <param name="left">
        /// The first branch to compare.
        /// </param>
        /// <param name="right">
        /// The second branch to compare.
        /// </param>
        /// <returns>
        /// <c>false</c> if the branches are equal; otherwise, <c>true</c>.
        /// </returns>
        public static bool operator!=(Branch left, Branch right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// An enumeration of things a branch argument can be.
    /// </summary>
    public enum BranchArgumentKind
    {
        /// <summary>
        /// The branch argument simply passes a value to a target
        /// basic block.
        /// </summary>
        Value,

        /// <summary>
        /// The branch argument passes the result of a 'try' flow's
        /// inner instruction to the target block. Only valid on success
        /// branches of 'try' flows.
        /// </summary>
        TryResult,

        /// <summary>
        /// The branch argument passes the exception thrown by a
        /// 'try' flow's inner instruction to the target block. Only
        /// valid on exception branches of 'try' flows.
        /// </summary>
        TryException
    }

    /// <summary>
    /// An argument to a branch.
    /// </summary>
    public struct BranchArgument
    {
        private BranchArgument(BranchArgumentKind kind, ValueTag value)
        {
            this = default(BranchArgument);
            this.Kind = kind;
            this.ValueOrNull = value;
        }

        /// <summary>
        /// Gets a description of this branch argument's kind.
        /// </summary>
        /// <returns>The branch argument's kind.</returns>
        public BranchArgumentKind Kind { get; private set; }

        /// <summary>
        /// Gets the value referred to by this branch argument. This is
        /// non-null if and only if this branch argument is a value.
        /// </summary>
        /// <returns>The value referred to by this branch argument.</returns>
        public ValueTag ValueOrNull { get; private set; }

        /// <summary>
        /// Tests if this branch argument is a value.
        /// </summary>
        public bool IsValue => Kind == BranchArgumentKind.Value;

        /// <summary>
        /// Tests if this branch argument is the result of a 'try' flow's
        /// inner instruction.
        /// </summary>
        public bool IsTryResult => Kind == BranchArgumentKind.TryResult;

        /// <summary>
        /// Tests if this branch argument is the exception thrown by a
        /// 'try' flow's inner instruction.
        /// </summary>
        public bool IsTryException => Kind == BranchArgumentKind.TryException;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsTryResult)
            {
                return "#result";
            }
            else if (IsTryException)
            {
                return "#exception";
            }
            else
            {
                return ValueOrNull.ToString();
            }
        }

        /// <summary>
        /// Creates a branch argument that passes a particular value
        /// to the branch's target block.
        /// </summary>
        /// <param name="value">The value to pass to the target block.</param>
        /// <returns>A branch argument.</returns>
        public static BranchArgument FromValue(ValueTag value)
        {
            return new BranchArgument(BranchArgumentKind.Value, value);
        }

        /// <summary>
        /// Gets a branch argument that represents the result of a 'try'
        /// flow's inner instruction.
        /// </summary>
        /// <returns>A branch argument.</returns>
        public static BranchArgument TryResult =>
            new BranchArgument(BranchArgumentKind.TryResult, null);

        /// <summary>
        /// Gets a branch argument that represents the exception thrown by
        /// a 'try' flow's inner instruction.
        /// </summary>
        /// <returns>A branch argument.</returns>
        public static BranchArgument TryException =>
            new BranchArgument(BranchArgumentKind.TryException, null);
    }
}