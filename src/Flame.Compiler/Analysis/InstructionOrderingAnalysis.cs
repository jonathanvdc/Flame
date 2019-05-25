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
        /// An instance of the conservative instruction ordering analysis.
        /// </summary>
        /// <value>A conservative instruction ordering analysis.</value>
        public static readonly ConservativeInstructionOrderingAnalysis Instance =
            new ConservativeInstructionOrderingAnalysis();

        /// <inheritdoc/>
        public InstructionOrdering Analyze(FlowGraph graph)
        {
            // This analysis imposes a partial ordering on instructions (the
            // dependency relation) based on the following rules:
            //
            //   1. Non-delayable exception-throwing instructions are
            //      totally ordered.
            //
            //   2. a. Value-reading instructions depend on value-writing
            //         instructions that refer to the same address.
            //      b. Value-writing instructions depend on value-writing
            //         instructions that refer to the same address.
            //      c. Exception-throwing instructions depend on value-writing
            //         instructions.
            //      d. Value-writing instructions depend on exception-throwing
            //         instructions.
            //      e. Value-writing instructions depend on value-reading
            //         instructions that refer to the same address.
            //
            //   3. All instructions depend on their arguments, provided
            //      that these arguments refer to instructions inside the
            //      same basic block.
            //
            //   4. Dependencies are transitive.

            var memorySpecs = graph.GetAnalysisResult<PrototypeMemorySpecs>();
            var exceptionSpecs = graph.GetAnalysisResult<InstructionExceptionSpecs>();
            var aliasAnalysis = graph.GetAnalysisResult<AliasAnalysisResult>();
            var delayability = graph.GetAnalysisResult<ExceptionDelayability>();

            var dependencies = new Dictionary<ValueTag, HashSet<ValueTag>>();
            foreach (var block in graph.BasicBlocks)
            {
                // `knownWrites` is a mapping of value-writing instructions to the
                // addresses they update.
                var knownWrites = new Dictionary<ValueTag, ValueTag>();
                // Ditto for `knownReads`, but it describes reads instead.
                var knownReads = new Dictionary<ValueTag, ValueTag>();
                // `unknownWrites` is the set of all writes to unknown addresses.
                var unknownWrites = new HashSet<ValueTag>();
                // `lastWrite` is the last write.
                ValueTag lastWrite = null;
                // `unknownReads` is the set of all reads from unknown addresses.
                var unknownReads = new HashSet<ValueTag>();
                // `lastRead` is the last read.
                ValueTag lastRead = null;
                // `lastNonDelayableThrower` is the last non-delayable exception-throwing
                // instruction.
                ValueTag lastNonDelayableThrower = null;
                // `lastThrower` is the last exception-throwing instruction.
                ValueTag lastThrower = null;

                foreach (var selection in block.NamedInstructions)
                {
                    var insnDependencies = new HashSet<ValueTag>();

                    var instruction = selection.Instruction;
                    var exceptionSpec = exceptionSpecs.GetExceptionSpecification(instruction);
                    var memSpec = memorySpecs.GetMemorySpecification(instruction.Prototype);

                    var oldLastThrower = lastThrower;
                    var oldLastRead = lastRead;

                    if (exceptionSpec.CanThrowSomething)
                    {
                        // Rule #2.c: Exception-throwing instructions depend on value-writing
                        // instructions.
                        insnDependencies.Add(lastWrite);
                        if (delayability.CanDelayExceptions(instruction.Prototype))
                        {
                            // Rule #1: Non-delayable exception-throwing instructions are
                            // totally ordered.
                            insnDependencies.Add(lastNonDelayableThrower);
                            lastNonDelayableThrower = selection;
                        }
                        lastThrower = selection;
                    }
                    if (memSpec.MayRead)
                    {
                        // Rule #2.a: Value-reading instructions depend on value-writing
                        // instructions that refer to the same address.
                        insnDependencies.UnionWith(unknownWrites);
                        if (memSpec is MemorySpecification.ArgumentRead)
                        {
                            var argReadSpec = (MemorySpecification.ArgumentRead)memSpec;
                            var readAddress = instruction.Arguments[argReadSpec.ParameterIndex];
                            foreach (var pair in knownWrites)
                            {
                                if (aliasAnalysis.GetAliasing(pair.Value, readAddress) != Aliasing.NoAlias)
                                {
                                    insnDependencies.Add(pair.Key);
                                }
                            }

                            // Update the set of known reads.
                            knownReads[selection] = selection;
                        }
                        else
                        {
                            insnDependencies.Add(lastWrite);

                            // Update the unknown read set.
                            unknownReads.Add(selection);
                        }
                        // Update the last read.
                        lastRead = selection;
                    }
                    if (memSpec.MayWrite)
                    {
                        // Rule #2.b: Value-writing instructions depend on value-writing
                        // instructions that refer to the same address.
                        // Rule #2.e: Value-writing instructions depend on value-reading
                        // instructions that refer to the same address.
                        insnDependencies.UnionWith(unknownWrites);
                        insnDependencies.UnionWith(unknownReads);
                        if (memSpec is MemorySpecification.ArgumentWrite)
                        {
                            var argWriteSpec = (MemorySpecification.ArgumentWrite)memSpec;
                            var writeAddress = instruction.Arguments[argWriteSpec.ParameterIndex];
                            foreach (var pair in knownWrites.Concat(knownReads))
                            {
                                if (pair.Key == selection.Tag)
                                {
                                    continue;
                                }

                                if (aliasAnalysis.GetAliasing(pair.Value, writeAddress) != Aliasing.NoAlias)
                                {
                                    insnDependencies.Add(pair.Key);
                                }
                            }

                            // Update the set of known writes.
                            knownWrites[selection] = writeAddress;
                        }
                        else
                        {
                            insnDependencies.Add(lastWrite);
                            insnDependencies.Add(oldLastRead);

                            // Update the unknown write set.
                            unknownWrites.Add(selection);
                        }
                        // Rule #2.d: Value-writing instructions depend on exception-throwing
                        // instructions.
                        insnDependencies.Add(oldLastThrower);

                        // Update the last write.
                        lastWrite = selection;
                    }

                    // Rule #3: all instructions depend on their arguments, provided
                    // that these arguments refer to instructions inside the
                    // same basic block.
                    foreach (var arg in instruction.Arguments)
                    {
                        NamedInstruction argInstruction;
                        if (graph.TryGetInstruction(arg, out argInstruction)
                            && argInstruction.Block.Tag == block.Tag)
                        {
                            insnDependencies.Add(arg);
                        }
                    }

                    // We might have added a `null` value tag to the set of dependencies.
                    // Adding it was harmless, but we need to rid ourselves of it before
                    // the dependency set is used in a situation where `null` is undesirable,
                    // like in the loop below. Ditto for self-dependencies.
                    insnDependencies.Remove(null);
                    insnDependencies.Remove(selection);

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
