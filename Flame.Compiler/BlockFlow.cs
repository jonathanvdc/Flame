using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;

namespace Flame.Compiler
{
    /// <summary>
    /// Describes control flow at the end of a basic block.
    /// </summary>
    public abstract class BlockFlow
    {
        /// <summary>
        /// Gets a list of inner instructions for this block flow.
        /// </summary>
        /// <returns>The inner instructions.</returns>
        public abstract IReadOnlyList<Instruction> Instructions { get; }

        /// <summary>
        /// Replaces this flow's inner instructions.
        /// </summary>
        /// <param name="instructions">The new instructions.</param>
        /// <returns>A new flow.</returns>
        public abstract BlockFlow WithInstructions(IReadOnlyList<Instruction> instructions);

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

        /// <summary>
        /// Gets a list of each branch's target.
        /// </summary>
        /// <value>A list of branch targets.</value>
        public IEnumerable<BasicBlockTag> BranchTargets
        {
            get
            {
                return Branches.Select(branch => branch.Target);
            }
        }

        /// <summary>
        /// Applies a mapping to all values referenced by instructions
        /// and branches in this block flow.
        /// </summary>
        /// <param name="mapping">A value-to-value mapping to apply.</param>
        /// <returns>A block flow.</returns>
        public BlockFlow MapValues(
            Func<ValueTag, ValueTag> mapping)
        {
            return this
                .WithInstructions(
                    Instructions.EagerSelect(insn => insn.MapArguments(mapping)))
                .WithBranches(
                    Branches.EagerSelect(branch => branch.MapArguments(mapping)));
        }

        /// <summary>
        /// Applies a mapping to all values referenced by instructions
        /// and branches in this block flow.
        /// </summary>
        /// <param name="mapping">
        /// A value-to-value mapping to apply.
        /// </param>
        /// <returns>A block flow.</returns>
        public BlockFlow MapValues(
            IReadOnlyDictionary<ValueTag, ValueTag> mapping)
        {
            return MapValues(arg =>
            {
                ValueTag result;
                if (mapping.TryGetValue(arg, out result))
                {
                    return result;
                }
                else
                {
                    return arg;
                }
            });
        }

        /// <summary>
        /// Applies a mapping to all basic blocks referenced by
        /// branches in this block flow.
        /// </summary>
        /// <param name="mapping">A block-to-block mapping to apply.</param>
        /// <returns>A block flow.</returns>
        public BlockFlow MapBlocks(
            Func<BasicBlockTag, BasicBlockTag> mapping)
        {
            return this
                .WithBranches(
                    Branches.EagerSelect(branch => branch.WithTarget(mapping(branch.Target))));
        }

        /// <summary>
        /// Applies a mapping to all basic blocks referenced by
        /// branches in this block flow.
        /// </summary>
        /// <param name="mapping">
        /// A block-to-block mapping to apply.
        /// </param>
        /// <returns>A block flow.</returns>
        public BlockFlow MapBlocks(
            IReadOnlyDictionary<BasicBlockTag, BasicBlockTag> mapping)
        {
            return MapBlocks(arg =>
            {
                BasicBlockTag result;
                if (mapping.TryGetValue(arg, out result))
                {
                    return result;
                }
                else
                {
                    return arg;
                }
            });
        }
    }
}
