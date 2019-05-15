using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;
using Flame.Compiler.Target;
using Flame.Constants;

namespace Flame.Clr.Emit
{
    /// <summary>
    /// An instruction stream builder for CIL instructions.
    /// </summary>
    internal sealed class CilInstructionStreamBuilder : StackInstructionStreamBuilder<CilCodegenInstruction>
    {
        /// <summary>
        /// Creates a CIL instruction stream builder.
        /// </summary>
        /// <param name="selector">An instruction selector.</param>
        private CilInstructionStreamBuilder(CilInstructionSelector selector)
            : base(selector)
        { }

        /// <summary>
        /// Creates a CIL instruction stream builder.
        /// </summary>
        /// <param name="selector">
        /// The instruction selector to use.
        /// </param>
        public static CilInstructionStreamBuilder Create(CilInstructionSelector selector)
        {
            return new CilInstructionStreamBuilder(selector);
        }

        /// <inheritdoc/>
        protected override IEnumerable<ValueTag> GetStackContentsOnEntry(BasicBlock block)
        {
            if (block.IsEntryPoint)
            {
                return Enumerable.Empty<ValueTag>();
            }
            else
            {
                return block.ParameterTags;
            }
        }

        /// <inheritdoc/>
        protected override bool ShouldMaterializeOnUse(NamedInstruction instruction)
        {
            // Materialize trivial constants on use.
            var proto = instruction.Prototype as ConstantPrototype;
            return proto != null && proto.Value != DefaultConstant.Instance;
        }
    }
}
