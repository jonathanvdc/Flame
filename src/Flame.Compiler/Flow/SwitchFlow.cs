using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Collections;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Flow
{
    /// <summary>
    /// Switch flow, which tries to match a value against a list of
    /// constants in cases and takes an appropriate branch based on
    /// which case is selected, if any.
    /// </summary>
    public sealed class SwitchFlow : BlockFlow
    {
        /// <summary>
        /// Creates switch flow.
        /// </summary>
        /// <param name="switchValue">
        /// An instruction that produces the value to switch on.
        /// </param>
        /// <param name="cases">
        /// A list of switch cases.
        /// </param>
        /// <param name="defaultBranch">
        /// A branch to take if none of the switch cases match
        /// the value being switched on.
        /// </param>
        /// <remarks>
        /// This constructor will simplify <paramref name="cases"/>
        /// by unifying cases that point to the same branch and
        /// eliminate cases that are associated with no constants
        /// or point to the default branch.
        /// </remarks>
        public SwitchFlow(
            Instruction switchValue,
            IReadOnlyList<SwitchCase> cases,
            Branch defaultBranch)
        {
            this.SwitchValue = switchValue;
            this.DefaultBranch = defaultBranch;

            // Iterate through the switch cases. Eliminate cases
            // that take the default branch and cases that have an
            // empty pattern. Create a list of branches and a case
            // list.
            var branchList = new List<Branch>();
            var branchPatterns = new Dictionary<Branch, ImmutableHashSet<Constant>>();

            foreach (var switchCase in cases)
            {
                if (switchCase.Values.Count == 0 || switchCase.Branch == DefaultBranch)
                {
                    // Eliminate trivial cases.
                    continue;
                }

                ImmutableHashSet<Constant> pattern;
                if (!branchPatterns.TryGetValue(switchCase.Branch, out pattern))
                {
                    pattern = ImmutableHashSet.Create<Constant>();
                    branchList.Add(switchCase.Branch);
                }
                // Unify constants from different cases that point
                // to the same branch.
                branchPatterns[switchCase.Branch] = pattern.Union(switchCase.Values);
            }

            var caseList = new List<SwitchCase>();
            foreach (var branch in branchList)
            {
                ImmutableHashSet<Constant> pattern;
                if (branchPatterns.TryGetValue(branch, out pattern))
                {
                    caseList.Add(new SwitchCase(pattern, branch));
                }
            }

            branchList.Add(DefaultBranch);

            this.Cases = caseList;
            this.cachedBranchList = branchList;
        }

        /// <summary>
        /// Gets an instruction that produces the value to switch on.
        /// </summary>
        /// <returns>An instruction that produces the value to switch on.</returns>
        public Instruction SwitchValue { get; private set; }

        /// <summary>
        /// Gets the list of switch cases in this switch flow.
        /// </summary>
        /// <returns>A list of switch cases.</returns>
        public IReadOnlyList<SwitchCase> Cases { get; private set; }

        /// <summary>
        /// Gets the default branch, which is only taken when no case matches.
        /// </summary>
        /// <returns>The default branch.</returns>
        public Branch DefaultBranch { get; private set; }

        /// <inheritdoc/>
        public override IReadOnlyList<Instruction> Instructions
            => new Instruction[] { SwitchValue };

        /// <inheritdoc/>
        public override IReadOnlyList<Branch> Branches => cachedBranchList;

        /// <summary>
        /// Tells if this switch flow represents if-else flow,
        /// that is, if it has a single case matching on a single value.
        /// </summary>
        public bool IsIfElseFlow => Cases.Count == 1 && Cases[0].Values.Count == 1;

        /// <summary>
        /// Tells if the switch flow can be implemented as a jump table,
        /// that is, its cases do not have any branches with arguments and all
        /// of its case values are integer constants.
        /// </summary>
        public bool IsJumpTable =>
            Cases.All(item =>
                item.Branch.Arguments.Count == 0
                && item.Values.All(val => val is IntegerConstant));

        /// <summary>
        /// Tells if this switch flow contains only integer constants.
        /// </summary>
        public bool IsIntegerSwitch =>
            Cases.All(item =>
                item.Values.All(val => val is IntegerConstant));

        /// <summary>
        /// Gets a mapping of values to branches for this switch.
        /// This mapping does not include the default branch.
        /// </summary>
        /// <value>A mapping of values to branches.</value>
        public IReadOnlyDictionary<Constant, Branch> ValueToBranchMap
        {
            get
            {
                var results = new Dictionary<Constant, Branch>();
                foreach (var switchCase in Cases)
                {
                    foreach (var value in switchCase.Values)
                    {
                        results[value] = switchCase.Branch;
                    }
                }
                return results;
            }
        }

        private IReadOnlyList<Branch> cachedBranchList;

        /// <inheritdoc/>
        public override BlockFlow WithBranches(IReadOnlyList<Branch> branches)
        {
            int branchCount = branches.Count;
            int caseCount = Cases.Count;

            ContractHelpers.Assert(
                branchCount == caseCount + 1,
                "Got '" + branchCount +
                "' branches when re-creating a switch statement, but expected '" +
                (caseCount + 1) + "'.");

            var newCases = new SwitchCase[caseCount];

            for (int i = 0; i < caseCount; i++)
            {
                newCases[i] = new SwitchCase(Cases[i].Values, branches[i]);
            }
            
            return new SwitchFlow(
                SwitchValue,
                newCases,
                branches[caseCount]);
        }

        /// <inheritdoc/>
        public override BlockFlow WithInstructions(IReadOnlyList<Instruction> instructions)
        {
            ContractHelpers.Assert(instructions.Count == 1, "Switch flow takes exactly one instruction.");
            var newSwitchValue = instructions[0];
            if (object.ReferenceEquals(newSwitchValue, SwitchValue))
            {
                return this;
            }
            else
            {
                return new SwitchFlow(newSwitchValue, Cases, DefaultBranch);
            }
        }

        /// <inheritdoc/>
        public override InstructionBuilder GetInstructionBuilder(
            BasicBlockBuilder block,
            int instructionIndex)
        {
            if (instructionIndex == 0)
            {
                return new SimpleFlowInstructionBuilder(block);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        /// <summary>
        /// Creates switch flow that corresponds to if-else flow on
        /// a Boolean condition.
        /// </summary>
        /// <param name="condition">
        /// An instruction that produces Boolean condition.
        /// </param>
        /// <param name="ifBranch">
        /// The 'if' branch, which is taken when the value produced by the
        /// Boolean condition is not false.
        /// </param>
        /// <param name="elseBranch">
        /// The 'else' branch, which is taken when the value produced by the
        /// Boolean condition is false.
        /// </param>
        /// <returns>
        /// Switch flow that corresponds to if-else flow.
        /// </returns>
        public static SwitchFlow CreateIfElse(
            Instruction condition,
            Branch ifBranch,
            Branch elseBranch)
        {
            var booleanSpec = condition.ResultType.GetIntegerSpecOrNull();
            return CreateConstantCheck(
                condition,
                booleanSpec == null
                    ? BooleanConstant.False
                    : new IntegerConstant(0, booleanSpec),
                elseBranch,
                ifBranch);
        }

        /// <summary>
        /// Creates switch flow that redirects control to one branch
        /// if a value equals <c>null</c> and to another branch otherwise.
        /// </summary>
        /// <param name="value">
        /// A value to compare to <c>null</c>.
        /// </param>
        /// <param name="nullBranch">
        /// The branch to which flow is redirected if <paramref name="value"/>
        /// equals <c>null</c>.
        /// </param>
        /// <param name="nonNullBranch">
        /// The branch to which flow is redirected if <paramref name="value"/>
        /// does not equal <c>null</c>.
        /// </param>
        /// <returns>
        /// Switch flow that corresponds to a null check.
        /// </returns>
        public static SwitchFlow CreateNullCheck(
            Instruction value,
            Branch nullBranch,
            Branch nonNullBranch)
        {
            return CreateConstantCheck(
                value,
                NullConstant.Instance,
                nullBranch,
                nonNullBranch);
        }

        /// <summary>
        /// Creates switch flow that redirects control to one branch
        /// if a value equals a particular constant and to another
        /// branch otherwise.
        /// </summary>
        /// <param name="value">
        /// The value to compare to a constant.
        /// </param>
        /// <param name="constant">
        /// The constant to compare the value to.
        /// </param>
        /// <param name="equalBranch">
        /// The branch to which flow is redirected if <paramref name="value"/>
        /// equals <paramref name="constant"/>.
        /// </param>
        /// <param name="notEqualBranch">
        /// The branch to which flow is redirected if <paramref name="value"/>
        /// does not equal <paramref name="constant"/>.
        /// </param>
        /// <returns>
        /// Switch flow that corresponds to a constant equality check.
        /// </returns>
        public static SwitchFlow CreateConstantCheck(
            Instruction value,
            Constant constant,
            Branch equalBranch,
            Branch notEqualBranch)
        {
            return new SwitchFlow(
                value,
                ImmutableList.Create<SwitchCase>(
                    new SwitchCase(constant, equalBranch)),
                notEqualBranch);
        }
    }

    /// <summary>
    /// A case in switch flow.
    /// </summary>
    public struct SwitchCase : IEquatable<SwitchCase>
    {
        /// <summary>
        /// Creates a switch case from a value and a branch.
        /// </summary>
        /// <param name="value">The value for the switch case.</param>
        /// <param name="branch">A branch for the switch case.</param>
        public SwitchCase(Constant value, Branch branch)
            : this(ImmutableHashSet.Create<Constant>(value), branch)
        { }

        /// <summary>
        /// Creates a switch case from a set of values and a branch.
        /// </summary>
        /// <param name="values">A set of values for the switch case.</param>
        /// <param name="branch">A branch for the switch case.</param>
        public SwitchCase(ImmutableHashSet<Constant> values, Branch branch)
        {
            this = default(SwitchCase);
            this.Values = values;
            this.Branch = branch;
        }

        /// <summary>
        /// Gets a set of all values for this switch case. If control reaches
        /// this switch case and any of these values match the value being
        /// switched on, then control is redirected to this switch case's
        /// branch target.
        /// </summary>
        /// <returns>The switch case's values.</returns>
        public ImmutableHashSet<Constant> Values { get; private set; }

        /// <summary>
        /// Gets the branch that is taken when any of the values in this
        /// switch case match the value being switched on.
        /// </summary>
        /// <returns>The switch case's branch.</returns>
        public Branch Branch { get; private set; }

        /// <summary>
        /// Tests if this switch case equals another switch case.
        /// </summary>
        /// <param name="other">
        /// A switch case to compare with this switch case.
        /// </param>
        /// <returns>
        /// <c>true</c> if the switch cases are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(SwitchCase other)
        {
            return Branch == other.Branch
                && Values.SetEquals(Values);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SwitchCase && Equals((SwitchCase)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = EnumerableComparer.HashUnorderedSet(Values);
            return EnumerableComparer.FoldIntoHashCode(hashCode, Branch.GetHashCode());
        }

        /// <summary>
        /// Tests if two switch cases are equal.
        /// </summary>
        /// <param name="left">
        /// The first switch cases to compare.
        /// </param>
        /// <param name="right">
        /// The second switch cases to compare.
        /// </param>
        /// <returns>
        /// <c>true</c> if the switch cases are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator==(SwitchCase left, SwitchCase right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Tests if two switch cases are not equal.
        /// </summary>
        /// <param name="left">
        /// The first switch cases to compare.
        /// </param>
        /// <param name="right">
        /// The second switch cases to compare.
        /// </param>
        /// <returns>
        /// <c>false</c> if the switch cases are equal; otherwise, <c>true</c>.
        /// </returns>
        public static bool operator!=(SwitchCase left, SwitchCase right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// An equality comparer that for structural switch flow equality.
    /// </summary>
    public sealed class StructuralSwitchFlowComparer : IEqualityComparer<SwitchFlow>
    {
        /// <summary>
        /// Tests if two switch flows are structurally equal.
        /// </summary>
        /// <param name="x">A first switch flow.</param>
        /// <param name="y">A second switch flow.</param>
        /// <returns>
        /// <c>true</c> if the switch flows are structurally equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(SwitchFlow x, SwitchFlow y)
        {
            if (x.SwitchValue != y.SwitchValue
                || x.DefaultBranch != y.DefaultBranch
                || x.Cases.Count != y.Cases.Count)
            {
                return false;
            }

            var branchPatternsForY = y.Cases.ToDictionary(
                item => item.Branch,
                item => item.Values);

            foreach (var item in x.Cases)
            {
                ImmutableHashSet<Constant> pattern;
                if (!branchPatternsForY.TryGetValue(item.Branch, out pattern)
                    || !pattern.SetEquals(item.Values))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes the hash code for a particular switch flow.
        /// </summary>
        /// <param name="obj">The switch flow to compute a hash code for.</param>
        /// <returns>A hash code.</returns>
        public int GetHashCode(SwitchFlow obj)
        {
            int hashCode = EnumerableComparer.HashUnorderedSet(obj.Cases);
            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                obj.DefaultBranch.GetHashCode());
            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                obj.SwitchValue.GetHashCode());
            return hashCode;
        }
    }
}
