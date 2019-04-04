using System.Collections.Generic;
using Flame.Compiler.Analysis;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that replaces redundant computations by copies
    /// based on the results of value numbering and dominator tree
    /// analyses.
    /// </summary>
    public sealed class GlobalValueNumbering : IntraproceduralOptimization
    {
        private GlobalValueNumbering()
        { }

        /// <summary>
        /// An instance of the global value numbering transform.
        /// </summary>
        public static readonly GlobalValueNumbering Instance = new GlobalValueNumbering();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // Compute the value numbering for the graph.
            var numbering = graph.GetAnalysisResult<ValueNumbering>();

            // Partition the set of all instructions into equivalence classes.
            var equivValues = new Dictionary<Instruction, HashSet<ValueTag>>(
                new ValueNumberingInstructionComparer(numbering));
            foreach (var insn in graph.NamedInstructions)
            {
                HashSet<ValueTag> valueSet;
                if (!equivValues.TryGetValue(insn.Instruction, out valueSet))
                {
                    equivValues[insn.Instruction] = valueSet = new HashSet<ValueTag>();
                }
                valueSet.Add(insn);
            }

            // Compute the dominator tree for the graph.
            var domTree = graph.GetAnalysisResult<DominatorTree>();

            // Replace instructions with copies to your heart's content.
            var builder = graph.ToBuilder();
            foreach (var insn in builder.Instructions)
            {
                // An instruction can be replaced with another instruction
                // if it is equivalent to that instruction and it is strictly
                // dominated by the other instruction.
                HashSet<ValueTag> valueSet;
                if (!equivValues.TryGetValue(insn.Instruction, out valueSet))
                {
                    continue;
                }

                foreach (var equivValue in valueSet)
                {
                    if (domTree.IsStrictlyDominatedBy(insn, equivValue))
                    {
                        insn.Instruction = Instruction.CreateCopy(insn.Instruction.ResultType, equivValue);
                        break;
                    }
                }
            }
            return builder.ToImmutable();
        }
    }
}
