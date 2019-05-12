using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Target;

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
        public CilInstructionStreamBuilder(CilInstructionSelector selector)
            : base(selector)
        { }

        /// <summary>
        /// Gets the contents of the evaluation stack just before a basic block's
        /// first instruction is executed.
        /// </summary>
        /// <param name="block">The basic block to inspect.</param>
        /// <returns>A sequence of values that represent the contents of the stack.</returns>
        protected override IEnumerable<ValueTag> GetStackContentsOnEntry(BasicBlock block)
        {
            return block.ParameterTags;
        }
    }
}
