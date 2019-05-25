using System.Collections.Generic;
using Loyc.Syntax;

namespace FlameMacros
{
    public sealed class RewriteRule
    {
        public RewriteRule(
            IReadOnlyList<InstructionPattern> pattern,
            IReadOnlyList<InstructionPattern> replacement,
            LNode condition)
        {
            this.Pattern = pattern;
            this.Replacement = replacement;
            this.Condition = condition;
        }

        /// <summary>
        /// Gets the pattern to match on.
        /// </summary>
        /// <value>The pattern to match on.</value>
        public IReadOnlyList<InstructionPattern> Pattern { get; private set; }

        /// <summary>
        /// Gets the replacement pattern to which the pattern
        /// may be rewritten.
        /// </summary>
        /// <value>The replacement pattern.</value>
        public IReadOnlyList<InstructionPattern> Replacement { get; private set; }

        /// <summary>
        /// Gets a condition that must be satisfied for the
        /// rewrite rule to be applicable.
        /// </summary>
        /// <value>A condition.</value>
        public LNode Condition { get; private set; }
    }
}
