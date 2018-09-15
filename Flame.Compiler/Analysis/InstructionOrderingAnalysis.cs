using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Captures the must-run-before relation between instructions.
    /// All instruction orderings that respect this relation are
    /// legal and computationally equivalent.
    /// </summary>
    public abstract class InstructionOrdering
    {
        /// <summary>
        /// Tells if the first instruction must run before the second
        /// instruction, assuming that both instructions are defined
        /// by the same basic block.
        /// </summary>
        /// <param name="first">
        /// The value tag of the first instruction to inspect.
        /// </param>
        /// <param name="second">
        /// The value tag of the second instruction to inspect.
        /// </param>
        /// <returns>
        /// <c>true</c> if the first instruction must run before the second
        /// instruction runs; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool MustRunBefore(ValueTag first, ValueTag second);
    }

    /// <summary>
    /// An instruction ordering implementation based on an explicit mapping
    /// of instructions to the closure of the instruction's dependent instructions.
    /// </summary>
    internal sealed class DependencyBasedInstructionOrdering : InstructionOrdering
    {
        /// <summary>
        /// Creates an instruction ordering based on an explicit dictionary
        /// specifying dependencies.
        /// </summary>
        /// <param name="dependencies">
        /// A mapping of values to their (recursive) dependencies.
        /// </param>
        public DependencyBasedInstructionOrdering(
            Dictionary<ValueTag, HashSet<ValueTag>> dependencies)
        {
            this.dependencies = dependencies;
        }

        private Dictionary<ValueTag, HashSet<ValueTag>> dependencies;

        /// <inheritdoc/>
        public override bool MustRunBefore(ValueTag first, ValueTag second)
        {
            return dependencies[second].Contains(first);
        }
    }

    /// <summary>
    /// A conservative analysis that determines the must-run-before
    /// relation between instructions. The must-run-before relation
    /// is determined by imposing a total ordering on effectful
    /// instructions.
    /// </summary>
    public sealed class ConservativeInstructionOrderingAnalysis : IFlowGraphAnalysis<InstructionOrdering>
    {
        private ConservativeInstructionOrderingAnalysis()
        { }

        /// <summary>
        /// Gets an instance of the conservative instruction ordering analysis.
        /// </summary>
        /// <returns>An instance of the conservative instruction ordering analysis.</returns>
        public static readonly ConservativeInstructionOrderingAnalysis Instance =
            new ConservativeInstructionOrderingAnalysis();

        /// <inheritdoc/>
        public InstructionOrdering Analyze(FlowGraph graph)
        {
            // This analysis is based on the following rules:
            //
            //   1. There is a total ordering between effectful
            //      instructions: effectful instructions can never
            //      be reordered wrt each other. That is, every
            //      effectful instruction depends on the previous
            //      effectful instruction.
            //
            //   2. `load` instructions depend on the last effectful
            //      instruction.
            //
            //   3. All instructions depend on their arguments, provided
            //      that these arguments refer to instructions inside the
            //      same basic block.
            //
            //   4. Dependencies are transitive.

            var effectfuls = graph.GetAnalysisResult<EffectfulInstructions>();

            var dependencies = new Dictionary<ValueTag, HashSet<ValueTag>>();
            foreach (var block in graph.BasicBlocks)
            {
                ValueTag lastEffectfulTag = null;
                foreach (var selection in block.Instructions)
                {
                    var insnDependencies = new HashSet<ValueTag>();

                    var instruction = selection.Instruction;
                    if (instruction.Prototype is LoadPrototype
                        && lastEffectfulTag != null)
                    {
                        // Rule #2: `load` instructions depend on the last effectful
                        // instruction.
                        if (lastEffectfulTag != null)
                        {
                            insnDependencies.Add(lastEffectfulTag);
                        }
                    }

                    if (effectfuls.Instructions.Contains(selection.Tag))
                    {
                        // Rule #1: every effectful instruction depends on the previous
                        // effectful instruction.
                        if (lastEffectfulTag != null)
                        {
                            insnDependencies.Add(lastEffectfulTag);
                        }
                        lastEffectfulTag = selection.Tag;
                    }

                    // Rule #3: all instructions depend on their arguments, provided
                    // that these arguments refer to instructions inside the
                    // same basic block.
                    foreach (var arg in instruction.Arguments)
                    {
                        if (graph.ContainsInstruction(arg)
                            && graph.GetInstruction(arg).Block.Tag == block.Tag)
                        {
                            insnDependencies.Add(arg);
                        }
                    }

                    // Rule #4: dependencies are transitive.
                    foreach (var item in insnDependencies.ToArray())
                    {
                        if (dependencies.ContainsKey(item))
                        {
                            insnDependencies.UnionWith(dependencies[item]);
                        }
                    }
                    dependencies[selection.Tag] = insnDependencies;
                }
            }
            return new DependencyBasedInstructionOrdering(dependencies);
        }

        /// <inheritdoc/>
        public InstructionOrdering AnalyzeWithUpdates(
            FlowGraph graph,
            InstructionOrdering previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            // TODO: some transformations don't invalidate the analysis.
            // Take them into account.
            return Analyze(graph);
        }
    }
}
