using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Constants;

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
        /// <param name="ifBranch">
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
            return new SwitchFlow(
                condition,
                ImmutableList<SwitchCase>.Empty.Add(
                    new SwitchCase(
                        ImmutableHashSet<Constant>.Empty.Add(
                            BooleanConstant.False),
                        elseBranch)),
                ifBranch);
        }
    }

    /// <summary>
    /// A case in switch flow.
    /// </summary>
    public struct SwitchCase
    {
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
    }
}