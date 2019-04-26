using System.Collections.Generic;
using System.Linq;

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
            new UnknownSpec(true, true);

        /// <summary>
        /// A memory access spec that identifies a read from an unknown location.
        /// </summary>
        /// <value>A memory access spec that represents an unknown read.</value>
        public static readonly MemoryAccessSpec UnknownRead =
            new UnknownSpec(true, false);

        /// <summary>
        /// A memory access spec that identifies a write to an unknown location.
        /// </summary>
        /// <value>A memory access spec that represents an unknown write.</value>
        public static readonly MemoryAccessSpec UnknownWrite =
            new UnknownSpec(false, true);

        private sealed class UnknownSpec : MemoryAccessSpec
        {
            public UnknownSpec(bool mayRead, bool mayWrite)
            {
                this.mayReadValue = mayRead;
                this.mayWriteValue = mayWrite;
            }

            private bool mayReadValue;
            private bool mayWriteValue;

            /// <inheritdoc/>
            public override bool MayRead => mayReadValue;

            /// <inheritdoc/>
            public override bool MayWrite => mayWriteValue;
        }

        /// <summary>
        /// A read from an address encoded by an argument.
        /// </summary>
        public sealed class ArgumentRead : MemoryAccessSpec
        {
            private ArgumentRead(int parameterIndex)
            {
                this.ParameterIndex = parameterIndex;
            }

            /// <summary>
            /// Gets the index of the parameter that corresponds to
            /// the argument that is read.
            /// </summary>
            /// <value>A parameter index.</value>
            public int ParameterIndex { get; private set; }

            /// <inheritdoc/>
            public override bool MayRead => true;

            /// <inheritdoc/>
            public override bool MayWrite => false;

            /// <summary>
            /// Creates a memory access spec that corresponds to a read
            /// from a particular argument.
            /// </summary>
            /// <param name="parameterIndex">
            /// The index of the parameter that corresponds to
            /// the argument that is read.
            /// </param>
            /// <returns>A memory access spec that represents an argument read.</returns>
            public static ArgumentRead Create(int parameterIndex)
            {
                return new ArgumentRead(parameterIndex);
            }
        }

        /// <summary>
        /// A write to an address encoded by an argument.
        /// </summary>
        public sealed class ArgumentWrite : MemoryAccessSpec
        {
            private ArgumentWrite(int parameterIndex)
            {
                this.ParameterIndex = parameterIndex;
            }

            /// <summary>
            /// Gets the index of the parameter that corresponds to
            /// the argument that is written to.
            /// </summary>
            /// <value>A parameter index.</value>
            public int ParameterIndex { get; private set; }

            /// <inheritdoc/>
            public override bool MayRead => false;

            /// <inheritdoc/>
            public override bool MayWrite => true;

            /// <summary>
            /// Creates a memory access spec that corresponds to a write
            /// to a particular argument.
            /// </summary>
            /// <param name="parameterIndex">
            /// The index of the parameter that corresponds to
            /// the argument that is written to.
            /// </param>
            /// <returns>A memory access spec that represents an argument write.</returns>
            public static ArgumentWrite Create(int parameterIndex)
            {
                return new ArgumentWrite(parameterIndex);
            }
        }

        /// <summary>
        /// A union of memory access specs.
        /// </summary>
        public sealed class Union : MemoryAccessSpec
        {
            private Union(IReadOnlyList<MemoryAccessSpec> elements)
            {
                this.Elements = elements;
            }

            /// <summary>
            /// Gets the memory access specs whose behavior is unified.
            /// </summary>
            /// <value>A list of memory access specs.</value>
            public IReadOnlyList<MemoryAccessSpec> Elements { get; private set; }

            /// <inheritdoc/>
            public override bool MayRead => Elements.Any(elem => elem.MayRead);

            /// <inheritdoc/>
            public override bool MayWrite => Elements.Any(elem => elem.MayWrite);

            /// <summary>
            /// Creates a memory access spec that represents the union of other
            /// memory access specs.
            /// </summary>
            /// <param name="elements">A sequence of memory access specs.</param>
            /// <returns>A union memory access spec.</returns>
            public static Union Create(IReadOnlyList<MemoryAccessSpec> elements)
            {
                return new Union(elements);
            }
        }
    }
}
