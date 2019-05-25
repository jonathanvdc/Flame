using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transform that looks for box instructions that
    /// are only ever unboxed and replaces them with alloca
    /// instructions. The transform is only applied to box
    /// instructions for which the unboxed pointers do not
    /// escape.
    /// </summary>
    public sealed class BoxToAlloca : IntraproceduralOptimization
    {
        private BoxToAlloca()
        { }

        /// <summary>
        /// An instance of the box-to-alloca transform.
        /// </summary>
        public static readonly BoxToAlloca Instance = new BoxToAlloca();

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var uses = graph.GetAnalysisResult<ValueUses>();

            // Figure out which unbox instructions are eligible.
            // TODO: implement a proper escape analysis.
            var nonescapingUnboxInstructions = new HashSet<ValueTag>();
            foreach (var instruction in graph.NamedInstructions)
            {
                if (instruction.Prototype is UnboxPrototype
                    && uses.GetFlowUses(instruction).Count == 0
                    && !uses.GetInstructionUses(instruction).Any(
                        tag => AllowsEscape(tag, instruction, graph)))
                {
                    nonescapingUnboxInstructions.Add(instruction);
                }
            }

            var builder = graph.ToBuilder();

            // Rewrite box and unbox instructions.
            var unboxReplacements = new Dictionary<ValueTag, ValueTag>();
            foreach (var instruction in builder.NamedInstructions)
            {
                var boxProto = instruction.Prototype as BoxPrototype;
                if (boxProto != null
                    && uses.GetFlowUses(instruction).Count == 0
                    && uses.GetInstructionUses(instruction).IsSubsetOf(
                        nonescapingUnboxInstructions))
                {
                    var boxedValue = boxProto.GetBoxedValue(instruction.Instruction);

                    // Replace the box with an alloca.
                    instruction.Instruction = Instruction.CreateAlloca(boxProto.ElementType);

                    // Initialize the alloca with the boxed value.
                    instruction.InsertAfter(
                        Instruction.CreateStore(
                            boxProto.ElementType,
                            instruction,
                            boxedValue));

                    // Record which unbox instructions wrap to which
                    // alloca instructions.
                    foreach (var use in uses.GetInstructionUses(instruction))
                    {
                        unboxReplacements[use] = instruction;
                    }
                }
            }

            // Replace all unbox instructions that use the box with
            // references to alloca instructions. Delete the unbox
            // instructions themselves.
            builder.ReplaceUses(unboxReplacements);
            builder.RemoveInstructionDefinitions(unboxReplacements.Keys);

            return builder.ToImmutable();
        }

        /// <summary>
        /// Tells if a value that uses a pointer allows said pointer to escape.
        /// </summary>
        /// <param name="value">A value in the flow graph.</param>
        /// <param name="pointer">
        /// A pointer in the flow graph that is used by <paramref name="value"/>.
        /// </param>
        /// <param name="graph">A flow graph.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="pointer"/> may escape; otherwise, <c>false</c>.
        /// </returns>
        private bool AllowsEscape(ValueTag value, ValueTag pointer, FlowGraph graph)
        {
            if (!graph.ContainsInstruction(value))
            {
                return true;
            }

            var instruction = graph.GetInstruction(value).Instruction;
            if (instruction.Prototype is LoadPrototype)
            {
                // Loads never allow anything to escape.
                return false;
            }
            else if (instruction.Prototype is StorePrototype)
            {
                // Stores to the pointer don't allow the pointer to escape,
                // but stores of the pointer to some other location may well
                // allow the pointer to escape.
                return ((StorePrototype)instruction.Prototype).GetValue(instruction) != pointer;
            }
            else
            {
                // Assume that all other instructions allow the pointer to
                // escape. This primitive escape analysis isn't refined enough
                // to actually track values, something we'd need to handle most
                // other benign instructions.
                return true;
            }
        }
    }
}
