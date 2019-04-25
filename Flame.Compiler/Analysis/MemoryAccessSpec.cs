using System.Collections.Generic;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A base class for descriptions of how an instruction interacts with memory.
    /// </summary>
    public abstract class MemoryAccessSpec
    {
        /// <summary>
        /// Tells if this memory access spec implies that the instruction it is attached
        /// to might read from some address.
        /// </summary>
        /// <returns><c>true</c> if the instruction might read; otherwise, <c>false</c>.</returns>
        public abstract bool MayRead { get; }

        /// <summary>
        /// Tells if this memory access spec implies that the instruction it is attached
        /// to might write to some address.
        /// </summary>
        /// <returns><c>true</c> if the instruction might write; otherwise, <c>false</c>.</returns>
        public abstract bool MayWrite { get; }

        /// <summary>
        /// A memory access spec that indicates that the memory access behavior of
        /// an instruction is unknown.
        /// </summary>
        /// <value>A memory access spec that represents an unknown operation.</value>
        public static readonly MemoryAccessSpec Unknown =
            new UnknownSpec();

        private sealed class UnknownSpec : MemoryAccessSpec
        {
            public UnknownSpec()
            { }

            /// <inheritdoc/>
            public override bool MayRead => true;

            /// <inheritdoc/>
            public override bool MayWrite => true;
        }

        private sealed class ArgumentRead : MemoryAccessSpec
        {
            public override bool MayRead => true;

            public override bool MayWrite => false;
        }
    }
}
