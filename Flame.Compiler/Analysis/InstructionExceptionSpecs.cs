using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// Maps instructions to their exception specifications.
    /// </summary>
    public abstract class InstructionExceptionSpecs
    {
        /// <summary>
        /// Gets the exception specification for a particular instruction.
        /// </summary>
        /// <param name="instruction">The instruction to examine.</param>
        /// <returns>An exception specification for <paramref name="instruction"/>.</returns>
        public abstract ExceptionSpecification GetExceptionSpecification(Instruction instruction);
    }

    /// <summary>
    /// An instruction exception specification mapping that trivially copies
    /// prototype exception specifications.
    /// </summary>
    public sealed class TrivialInstructionExceptionSpecs : InstructionExceptionSpecs
    {
        /// <summary>
        /// Creates instruction exception specification rules that
        /// simply copy instruction prototype exception specifications.
        /// </summary>
        /// <param name="exceptionSpecs">
        /// Prototype exception specification rules.
        /// </param>
        public TrivialInstructionExceptionSpecs(PrototypeExceptionSpecs exceptionSpecs)
        {
            this.ExceptionSpecs = exceptionSpecs;
        }

        /// <summary>
        /// Exception specification rules for instruction prototypes.
        /// </summary>
        /// <value>Exception specification rules.</value>
        public PrototypeExceptionSpecs ExceptionSpecs { get; private set; }

        /// <inheritdoc/>
        public override ExceptionSpecification GetExceptionSpecification(Instruction instruction)
        {
            return ExceptionSpecs.GetExceptionSpecification(instruction.Prototype);
        }
    }
}
