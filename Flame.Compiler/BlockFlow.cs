using System.Collections.Generic;

namespace Flame.Compiler
{
    /// <summary>
    /// A branch to a particular block that passes a list
    /// of values as arguments.
    /// </summary>
    public sealed class Branch
    {
        /// <summary>
        /// Creates a branch that targets a particular block and
        /// passes a list of arguments.
        /// </summary>
        /// <param name="target">The target block.</param>
        /// <param name="arguments">
        /// A list of arguments to pass to the target block.
        /// </param>
        public Branch(BasicBlockTag target, IReadOnlyList<ValueTag> arguments)
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
        public IReadOnlyList<ValueTag> Arguments { get; private set; }

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
        public Branch WithArguments(IReadOnlyList<ValueTag> arguments)
        {
            return new Branch(Target, arguments);
        }
    }

    /// <summary>
    /// Describes control flow at the end of a basic block.
    /// </summary>
    public abstract class BlockFlow
    {
        /// <summary>
        /// Gets a list of values this flow takes as arguments.
        /// This list does not include the values passed to target
        /// blocks by branches.
        /// </summary>
        /// <returns>The values taken as arguments.</returns>
        public abstract IReadOnlyList<ValueTag> Arguments { get; }

        /// <summary>
        /// Replaces this flow's arguments with a particular
        /// list of arguments.
        /// </summary>
        /// <param name="arguments">The new arguments.</param>
        /// <returns>A new flow.</returns>
        public abstract BlockFlow WithArguments(IReadOnlyList<ValueTag> arguments);

        /// <summary>
        /// Gets a list of branches this flow may take.
        /// </summary>
        /// <returns>A list of potential branches.</returns>
        public abstract IReadOnlyList<Branch> Branches { get; }

        /// <summary>
        /// Replaces this flow's branches with a particular
        /// list of branches.
        /// </summary>
        /// <param name="branches">The new branches.</param>
        /// <returns>A new flow.</returns>
        public abstract BlockFlow WithBranches(IReadOnlyList<Branch> branches);
    }
}