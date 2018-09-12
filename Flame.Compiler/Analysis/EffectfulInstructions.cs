using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// The set of instructions in a graph that may have side-effects.
    /// </summary>
    public struct EffectfulInstructions
    {
        /// <summary>
        /// Creates a set of effectful instructions.
        /// </summary>
        /// <param name="instructions">
        /// The set of effectful instructions to encapsulate.
        /// </param>
        public EffectfulInstructions(ImmutableHashSet<ValueTag> instructions)
        {
            this.Instructions = instructions;
        }

        /// <summary>
        /// Gets the set of effectful instructions as an immutable hash set.
        /// </summary>
        /// <value>The set of effectful instructions.</value>
        public ImmutableHashSet<ValueTag> Instructions { get; private set; }
    }

    /// <summary>
    /// An analysis that produces the set of all effectful instructions
    /// in a graph.
    /// </summary>
    public sealed class EffectfulInstructionAnalysis : IFlowGraphAnalysis<EffectfulInstructions>
    {
        /// <summary>
        /// Creates an effectful instruction analysis based on the default
        /// effectfulness predicate.
        /// </summary>
        public EffectfulInstructionAnalysis()
            : this(DefaultIsEffectfulImpl)
        { }

        /// <summary>
        /// Creates an effectful instruction analysis based on a
        /// predicate that tells if instructions are effectful.
        /// </summary>
        /// <param name="isEffectful">
        /// A predicate that takes an instruction and tells if it is
        /// effectful or not.
        /// </param>
        public EffectfulInstructionAnalysis(
            Predicate<Instruction> isEffectful)
        {
            this.IsEffectful = isEffectful;
        }

        /// <summary>
        /// Tells if a particular instruction is effectful.
        /// </summary>
        /// <value>
        /// A predicate that takes an instruction and tells if it is
        /// effectful or not.
        /// </value>
        public Predicate<Instruction> IsEffectful { get; private set; }

        /// <inheritdoc/>
        public EffectfulInstructions Analyze(FlowGraph graph)
        {
            var results = ImmutableHashSet.CreateBuilder<ValueTag>();
            foreach (var instruction in graph.Instructions)
            {
                if (IsEffectful(instruction.Instruction))
                {
                    results.Add(instruction.Tag);
                }
            }
            return new EffectfulInstructions(results.ToImmutable());
        }

        /// <inheritdoc/>
        public EffectfulInstructions AnalyzeWithUpdates(
            FlowGraph graph,
            EffectfulInstructions previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            var effectfulSet = previousResult.Instructions.ToBuilder();
            foreach (var item in updates)
            {
                if (item is BasicBlockFlowUpdate
                    || item is BasicBlockParametersUpdate
                    || item is SetEntryPointUpdate)
                {
                    // These updates don't affect the set of effectful instructions,
                    // so we can just ignore them.
                    continue;
                }
                else if (item is AddInstructionUpdate)
                {
                    var addUpdate = (AddInstructionUpdate)item;
                    if (IsEffectful(addUpdate.Instruction))
                    {
                        effectfulSet.Add(addUpdate.Tag);
                    }
                }
                else if (item is RemoveInstructionUpdate)
                {
                    var removeUpdate = (AddInstructionUpdate)item;
                    effectfulSet.Remove(removeUpdate.Tag);
                }
                else if (item is ReplaceInstructionUpdate)
                {
                    var replaceUpdate = (ReplaceInstructionUpdate)item;
                    if (IsEffectful(replaceUpdate.Instruction))
                    {
                        effectfulSet.Add(replaceUpdate.Tag);
                    }
                    else
                    {
                        effectfulSet.Remove(replaceUpdate.Tag);
                    }
                }
                else
                {
                    // Other updates may affect the set of effectful instructions,
                    // so we'll re-analyze if we encounter one.
                    return Analyze(graph);
                }
            }
            return new EffectfulInstructions(effectfulSet.ToImmutable());
        }

        private static bool DefaultIsEffectfulImpl(Instruction instruction)
        {
            var proto = instruction.Prototype;
            if (proto.ExceptionSpecification.CanThrowSomething
                || proto is StorePrototype
                || proto is CallPrototype
                || proto is NewObjectPrototype)
            {
                // TODO: consider method attributes. Some calls may
                // not have side-effects and may be marked as such.
                return true;
            }
            else
            {
                // TODO: support effectful intrinsics that do not throw.
                // At the moment, all non-throwing intrinsics are assumed
                // to not be effectful, but that's not exactly ideal.
                return false;
            }
        }
    }
}
