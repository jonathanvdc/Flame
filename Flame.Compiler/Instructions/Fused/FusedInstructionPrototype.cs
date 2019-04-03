using System;
using System.Collections.Generic;

namespace Flame.Compiler.Instructions.Fused
{
    /// <summary>
    /// A prototype for a fused instruction: a special composite
    /// instruction that is entirely equivalent to a sequence of
    /// core instructions.
    /// Fused instructions are mostly useful for making back-ends
    /// emit better code.
    /// </summary>
    public abstract class FusedInstructionPrototype : InstructionPrototype
    {
        /// <summary>
        /// Creates a fused instruction prototype.
        /// </summary>
        public FusedInstructionPrototype()
        {
            this.resultTypeCache = new Lazy<IType>(GetResultType);
        }

        private Lazy<IType> resultTypeCache;

        /// <summary>
        /// Expands this fused instruction to an equivalent nonempty sequence
        /// of core instructions. The instance itself must be replaced by
        /// another instruction. Instruction expansion must be formulaic:
        /// it cannot depend on the rest of the control-flow graph.
        /// </summary>
        /// <param name="instance">
        /// An instance of this prototype to expand.
        /// </param>
        public abstract void Expand(NamedInstructionBuilder instance);

        /// <inheritdoc/>
        public sealed override IType ResultType => resultTypeCache.Value;

        private IType GetResultType()
        {
            // Create a fake instance of this prototype, add it to
            // a fake CFG, expand it and then get the result's
            // instruction's type.

            // Create the fake instruction.
            var args = new ValueTag[ParameterCount];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = new ValueTag();
            }
            var instruction = Instantiate(args);

            // Add it to a fake CFG.
            var builder = new FlowGraphBuilder();
            var block = builder.GetBasicBlock(builder.EntryPointTag);
            var insn = block.AppendInstruction(instruction);

            // Expand the instruction.
            Expand(insn);

            // Get the instruction's result type.
            return insn.Instruction.ResultType;
        }

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body)
        {
            // Create a mock basic block that contains the instruction,
            // expand the instruction and check conformance.
            var builder = body.Implementation.ToBuilder();
            var block = builder.AddBasicBlock();
            var insn = block.AppendInstruction(instance);
            Expand(insn);

            var results = new List<string>();
            foreach (var item in block.NamedInstructions)
            {
                results.AddRange(
                    item.Prototype.CheckConformance(
                        item.Instruction, body));
            }
            return results;
        }
    }
}
