using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;

namespace Flame.Compiler.Target
{
    /// <summary>
    /// A target-specific peephole optimizer: an optimizer that walks
    /// through a linear sequence of target-specific instructions,
    /// recognizes patterns and rewrites small batches of instructions.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of target-specific instruction to optimize.
    /// </typeparam>
    /// <typeparam name="TExternalRef">
    /// The type of an external reference to instructions.
    /// </typeparam>
    public class PeepholeOptimizer<TInstruction, TExternalRef>
    {
        /// <summary>
        /// Creates a peephole optimizer that applies a set of rewrite rules.
        /// </summary>
        /// <param name="rules">
        /// The rewrite rules to apply.
        /// </param>
        public PeepholeOptimizer(
            IReadOnlyList<PeepholeRewriteRule<TInstruction>> rules)
        {
            this.Rules = rules;
        }

        /// <summary>
        /// Gets a list of all rewrite rules used by this
        /// peephole optimizer.
        /// </summary>
        /// <value>
        /// A list of rewrite rules.
        /// </value>
        public IReadOnlyList<PeepholeRewriteRule<TInstruction>> Rules { get; private set; }

        /// <summary>
        /// Gets a list of all instructions to which a particular instruction
        /// may branch.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that may branch to other instructions.
        /// </param>
        /// <returns>
        /// A list of branch targets.
        /// </returns>
        protected virtual IEnumerable<TInstruction> GetBranchTargets(
            TInstruction instruction)
        {
            return EmptyArray<TInstruction>.Value;
        }

        /// <summary>
        /// Gets a list of all instructions referenced by a particular
        /// external instruction reference.
        /// </summary>
        /// <param name="externalRef">The external reference to examine.</param>
        /// <returns>A list of referenced instructions.</returns>
        protected virtual IEnumerable<TInstruction> GetInstructionReferences(
            TExternalRef externalRef)
        {
            return EmptyArray<TInstruction>.Value;
        }

        /// <summary>
        /// Rewrites an instruction's branch targets.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to rewrite.
        /// </param>
        /// <param name="branchTargetMap">
        /// A mapping of old branch target instructions to new
        /// branch target instructions.
        /// </param>
        /// <returns>
        /// A modified or new instruction.
        /// </returns>
        protected virtual TInstruction RewriteBranchTargets(
            TInstruction instruction,
            IReadOnlyDictionary<TInstruction, TInstruction> branchTargetMap)
        {
            return instruction;
        }

        /// <summary>
        /// Rewrites an external reference's referenced instructions.
        /// </summary>
        /// <param name="externalRef">
        /// The reference to rewrite.
        /// </param>
        /// <param name="referenceMap">
        /// A mapping of old referenced instructions to new
        /// referenced instructions.
        /// </param>
        /// <returns>
        /// A modified or new external reference.
        /// </returns>
        protected virtual TExternalRef RewriteInstructionReferences(
            TExternalRef externalRef,
            IReadOnlyDictionary<TInstruction, TInstruction> referenceMap)
        {
            return externalRef;
        }

        /// <summary>
        /// Optimizes a linear sequence of instructions by applying
        /// rewrite rules.
        /// </summary>
        /// <param name="instructions">
        /// The instructions to optimize.
        /// </param>
        /// <returns>
        /// A linear sequence of optimized instructions.
        /// </returns>
        public IReadOnlyList<TInstruction> Optimize(
            IReadOnlyList<TInstruction> instructions)
        {
            IReadOnlyList<TExternalRef> newExternalRefs;
            return Optimize(instructions, EmptyArray<TExternalRef>.Value, out newExternalRefs);
        }

        /// <summary>
        /// Optimizes a linear sequence of instructions by applying
        /// rewrite rules.
        /// </summary>
        /// <param name="instructions">
        /// The instructions to optimize.
        /// </param>
        /// <param name="externalRefs">
        /// A list of external references to instructions.
        /// </param>
        /// <param name="newExternalRefs">
        /// A list of rewritten external references to instructions.
        /// </param>
        /// <returns>
        /// A linear sequence of optimized instructions.
        /// </returns>
        public IReadOnlyList<TInstruction> Optimize(
            IReadOnlyList<TInstruction> instructions,
            IReadOnlyList<TExternalRef> externalRefs,
            out IReadOnlyList<TExternalRef> newExternalRefs)
        {
            var results = new LinkedList<TInstruction>(instructions);

            // Create a trivial branch target replacement map.
            var replacedBranchTargets = new Dictionary<TInstruction, HashSet<TInstruction>>();
            foreach (var insn in instructions)
            {
                foreach (var target in GetBranchTargets(insn))
                {
                    replacedBranchTargets[target] = new HashSet<TInstruction> { target };
                }
            }
            foreach (var externalRef in externalRefs)
            {
                foreach (var target in GetInstructionReferences(externalRef))
                {
                    replacedBranchTargets[target] = new HashSet<TInstruction> { target };
                }
            }

            // Allocate a temporary array for storing instructions in.
            var tempInsnArray = new TInstruction[Rules.Max(rule => rule.Pattern.Count)];

            // Rewrite rules might enable other rewrite rules that
            // occur prior to the rewritten subsequences. Iterate
            // until the instruction stream stops changing.
            bool changes = true;
            while (changes)
            {
                changes = false;
                // Walk through the entire instruction list and rewrite
                // as much as we can.
                var current = results.First;
                while (current != null)
                {
                    // Apply rules to the current node until we can't anymore.
                    while (current != null
                        && TryApplyRule(current, out current, tempInsnArray, replacedBranchTargets))
                    {
                        changes = true;
                    }

                    if (current != null)
                    {
                        current = current.Next;
                    }
                }
            }

            // Turn the branch target replacement map (new targets -> old targets)
            // into an old targets -> new targets mapping.
            var branchTargetMap = new Dictionary<TInstruction, TInstruction>();
            foreach (var pair in replacedBranchTargets)
            {
                foreach (var item in pair.Value)
                {
                    branchTargetMap.Add(item, pair.Key);
                }
            }

            // Rewrite external instruction references.
            newExternalRefs = externalRefs
                .Select(externalRef => RewriteInstructionReferences(externalRef, branchTargetMap))
                .ToArray();

            // Rewrite branch targets and return.
            return results
                .Select(insn => RewriteBranchTargets(insn, branchTargetMap))
                .ToArray();
        }


        /// <summary>
        /// Tries to apply the longest rule applicable to
        /// a sequence of instructions starting at a
        /// given instruction.
        /// </summary>
        /// <param name="first">
        /// The first instruction of the sequence of instructions
        /// to rewrite.
        /// </param>
        /// <param name="newFirst">
        /// The new node pointing to the first instruction.
        /// </param>
        /// <param name="instructionArray">
        /// A temporary array for storing instructions. Must be at least
        /// as large as the longest pattern.
        /// </param>
        /// <param name="replacedBranchTargets">
        /// A mapping of new branch target instructions to the
        /// branch target instructions they replace.
        /// </param>
        /// <returns>
        /// <c>true</c> if a rewrite rule is applied;
        /// otherwise, <c>false</c>.
        /// </returns>
        private bool TryApplyRule(
            LinkedListNode<TInstruction> first,
            out LinkedListNode<TInstruction> newFirst,
            TInstruction[] instructionArray,
            Dictionary<TInstruction, HashSet<TInstruction>> replacedBranchTargets)
        {
            foreach (var rule in Rules)
            {
                if (TryApplyRule(rule, first, out newFirst, instructionArray, replacedBranchTargets))
                {
                    return true;
                }
            }
            newFirst = first;
            return false;
        }

        /// <summary>
        /// Tries to apply a specific rule to a sequence
        /// of instructions that starts at a particular
        /// instruction.
        /// </summary>
        /// <param name="rule">
        /// The rule to apply.
        /// </param>
        /// <param name="first">
        /// The first instruction to rewrite.
        /// </param>
        /// <param name="newFirst">
        /// The new node pointing to the first instruction.
        /// </param>
        /// <param name="instructionArray">
        /// A temporary array for storing instructions. Must be at least
        /// as large as the pattern to match.
        /// </param>
        /// <param name="replacedBranchTargets">
        /// A mapping of new branch target instructions to the
        /// branch target instructions they replace.
        /// </param>
        /// <returns>
        /// <c>true</c> if the rule was successfully applied; otherwise, <c>false</c>.
        /// </returns>
        private static bool TryApplyRule(
            PeepholeRewriteRule<TInstruction> rule,
            LinkedListNode<TInstruction> first,
            out LinkedListNode<TInstruction> newFirst,
            TInstruction[] instructionArray,
            Dictionary<TInstruction, HashSet<TInstruction>> replacedBranchTargets)
        {
            // Ensure that the pattern matches.
            var oldFirstInstruction = first.Value;
            var preFirst = first.Previous;
            var linkedList = first.List;
            var current = first;
            int patternLength = rule.Pattern.Count;
            for (int i = 0; i < patternLength; i++)
            {
                var value = current.Value;
                if (current == null
                    || !rule.Pattern[i](value)
                    || (current != first && replacedBranchTargets.ContainsKey(value)))
                {
                    // A pattern may not be applicable for one of three reasons:
                    //
                    //   1. The subsequence being examined is shorter than the
                    //      pattern itself.
                    //
                    //   2. An element of the pattern does not match.
                    //
                    //   3. An instruction in the subsequence is a branch target.
                    //      The first instruction in the subsequence is exempt from
                    //      this rule.
                    //
                    // These three rules are encoded in the `if` condition above.
                    newFirst = first;
                    return false;
                }
                instructionArray[i] = value;
                current = current.Next;
            }

            // Construct the sequence of instructions to replace.
            IReadOnlyList<TInstruction> sequence =
                new ArraySegment<TInstruction>(instructionArray, 0, patternLength);

            // Check if the sequence adheres to the macro-pattern.
            if (!rule.MacroPattern(sequence))
            {
                newFirst = first;
                return false;
            }

            // Actually rewrite the pattern.
            var rewritten = rule.Rewrite(sequence);

            var newLength = rewritten.Count;
            var reusableNodeCount = Math.Min(patternLength, newLength);

            // Try to reuse as many linked list nodes as possible.
            current = first;
            for (int i = 0; i < reusableNodeCount; i++)
            {
                current.Value = rewritten[i];
                current = current.Next;
            }

            // Delete any excess nodes.
            var deleteNodeCount = patternLength - reusableNodeCount;
            for (int i = 0; i < deleteNodeCount; i++)
            {
                var nextNode = current.Next;
                linkedList.Remove(current);
                current = nextNode;
            }

            // Insert additional nodes if necessary.
            for (int i = reusableNodeCount; i < newLength; i++)
            {
                current = linkedList.AddAfter(current, rewritten[i]);
            }

            // Figure out what the new 'first' instruction is.
            newFirst = preFirst == null ? linkedList.First : preFirst.Next;

            if (!object.Equals(newFirst.Value, oldFirstInstruction))
            {
                // Update the branch target map.
                HashSet<TInstruction> oldBranchTargets;
                if (replacedBranchTargets.TryGetValue(oldFirstInstruction, out oldBranchTargets))
                {
                    HashSet<TInstruction> newBranchTargets;
                    if (!replacedBranchTargets.TryGetValue(newFirst.Value, out newBranchTargets))
                    {
                        replacedBranchTargets[newFirst.Value] = newBranchTargets = new HashSet<TInstruction>();
                    }

                    newBranchTargets.Add(oldFirstInstruction);
                    newBranchTargets.UnionWith(oldBranchTargets);

                    replacedBranchTargets.Remove(oldFirstInstruction);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// A rewrite rule as used by a peephole optimizer.
    /// </summary>
    /// <typeparam name="TInstruction">
    /// The type of target-specific instruction to rewrite.
    /// </typeparam>
    public struct PeepholeRewriteRule<TInstruction>
    {
        /// <summary>
        /// Creates a peephole rewrite rule.
        /// </summary>
        /// <param name="pattern">
        /// The pattern to match on, specified as a list
        /// of predicates. The rewrite rule is considered to
        /// be applicable if and only if every pattern in the
        /// list is a match.
        /// </param>
        /// <param name="rewrite">
        /// A function that rewrites instructions that match
        /// the pattern.
        /// </param>
        public PeepholeRewriteRule(
            IReadOnlyList<Predicate<TInstruction>> pattern,
            Func<IReadOnlyList<TInstruction>, IReadOnlyList<TInstruction>> rewrite)
            : this(pattern, seq => true, rewrite)
        { }

        /// <summary>
        /// Creates a peephole rewrite rule.
        /// </summary>
        /// <param name="pattern">
        /// The pattern to match on, specified as a list
        /// of predicates. The rewrite rule only considered to
        /// be applicable if every pattern in the list is a match.
        /// list is a match.
        /// </param>
        /// <param name="macroPattern">
        /// A "macro-pattern" that decides if a sequence of
        /// instructions can be rewritten by the rewrite rule,
        /// assuming that every instruction in the sequence
        /// already adheres to <paramref name="pattern"/>.
        /// </param>
        /// <param name="rewrite">
        /// A function that rewrites instructions that match
        /// the pattern.
        /// </param>
        public PeepholeRewriteRule(
            IReadOnlyList<Predicate<TInstruction>> pattern,
            Predicate<IReadOnlyList<TInstruction>> macroPattern,
            Func<IReadOnlyList<TInstruction>, IReadOnlyList<TInstruction>> rewrite)
        {
            this.Pattern = pattern;
            this.MacroPattern = macroPattern;
            this.Rewrite = rewrite;
        }

        /// <summary>
        /// Gets the pattern to match on, specified as a list
        /// of predicates. The rewrite rule is only considered to
        /// be applicable if every pattern in the list is a match.
        /// </summary>
        /// <value>
        /// A list of instruction-matching predicates.
        /// </value>
        public IReadOnlyList<Predicate<TInstruction>> Pattern { get; private set; }

        /// <summary>
        /// Gets a "macro-pattern" that decides if a sequence of
        /// instructions can be rewritten by the rewrite rule,
        /// assuming that every instruction in the sequence
        /// already adheres to the pattern.
        /// </summary>
        /// <value>A predicate on a sequence of instructions.</value>
        public Predicate<IReadOnlyList<TInstruction>> MacroPattern { get; private set; }

        /// <summary>
        /// Rewrites a list of instructions matching the pattern
        /// specified by this rewrite rule.
        /// </summary>
        /// <value>
        /// A function that rewrites instructions.
        /// </value>
        public Func<IReadOnlyList<TInstruction>, IReadOnlyList<TInstruction>> Rewrite { get; private set; }
    }
}
