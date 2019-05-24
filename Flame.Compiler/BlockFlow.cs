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
        /// Gets an instruction builder for the nth anonymous
        /// instruction in this block flow.
        /// </summary>
        /// <param name="block">The block that defines this flow.</param>
        /// <param name="instructionIndex">
        /// The index of the anonymous instruction to create a builder for.
        /// </param>
        /// <returns>
        /// An instruction builder for an anonymous instruction.
        /// </returns>
        public abstract InstructionBuilder GetInstructionBuilder(
            BasicBlockBuilder block,
            int instructionIndex);

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
        /// Gets a sequence of all values that are used in this block flow.
        /// Multiply used values are appropriately duplicated.
        /// </summary>
        /// <value>A sequence of values.</value>
        public IEnumerable<ValueTag> Values
        {
            get
            {
                return Instructions.SelectMany(insn => insn.Arguments)
                    .Concat(
                        Branches.SelectMany(branch =>
                            branch.Arguments
                                .Where(arg => arg.IsValue)
                                .Select(arg => arg.ValueOrNull)));
            }
        }

        /// <summary>
        /// Gets instruction builders for all anonymous instructions
        /// in this block flow.
        /// </summary>
        /// <param name="block">The block that defines this flow.</param>
        /// <returns>
        /// A sequence of instruction builders for anonymous instructions.
        /// </returns>
        public IEnumerable<InstructionBuilder> GetInstructionBuilders(BasicBlockBuilder block)
        {
            return Instructions.Select((insn, i) => GetInstructionBuilder(block, i));
        }

        /// <summary>
        /// Applies a mapping to all values referenced by instructions
        /// and branches in this block flow.
        /// </summary>
        /// <param name="mapping">A value-to-value mapping to apply.</param>
        /// <returns>Block flow.</returns>
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
        /// <returns>Block flow.</returns>
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
        /// <returns>Block flow.</returns>
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
        /// <returns>Block flow.</returns>
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

        /// <summary>
        /// Applies a mapping to all branches in this basic block.
        /// </summary>
        /// <param name="mapping">The mapping to apply.</param>
        /// <returns>Block flow.</returns>
        public BlockFlow MapBranches(Func<Branch, Branch> mapping)
        {
            return WithBranches(Branches.EagerSelect(mapping));
        }

        /// <summary>
        /// Applies a mapping to all branch arguments in this flow.
        /// </summary>
        /// <param name="mapping">
        /// A argument-to-argument mapping to apply.
        /// </param>
        /// <returns>Block flow.</returns>
        public BlockFlow MapArguments(
            Func<BranchArgument, BranchArgument> mapping)
        {
            return this
                .WithBranches(
                    Branches.EagerSelect(branch => branch.MapArguments(mapping)));
        }

        /// <summary>
        /// Applies a mapping to all branch arguments in this flow.
        /// </summary>
        /// <param name="mapping">
        /// A argument-to-argument mapping to apply.
        /// </param>
        /// <returns>Block flow.</returns>
        public BlockFlow MapArguments(
            IReadOnlyDictionary<BranchArgument, BranchArgument> mapping)
        {
            return MapArguments(arg =>
            {
                BranchArgument result;
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
