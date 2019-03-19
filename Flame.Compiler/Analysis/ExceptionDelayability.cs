using System.Collections.Generic;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A base class for rules about whether or not exceptions
    /// thrown by particular types of instructions may be delayed
    /// until the value they produce is used by an effectful
    /// instruction.
    /// </summary>
    public abstract class ExceptionDelayability
    {
        /// <summary>
        /// Tells if it is permissible to delay exceptions thrown by a
        /// particular instruction until the instruction's result is used
        /// by an effectful instruction.
        /// If the instruction's result is never used that way,
        /// the exception may even be deleted altogether.
        /// </summary>
        /// <param name="prototype">
        /// An instruction prototype to examine.
        /// </param>
        /// <returns>
        /// <c>true</c> if exceptions thrown by instances of <paramref name="prototype"/>
        /// may be delayed until the instances' values are used by effectful instructions;
        /// otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanDelayExceptions(InstructionPrototype prototype);

        // If you're wondering about why it's useful to have the information
        // returned by implementations of this class, here's an example where
        // this kind of information comes in really handy.
        //
        // Suppose that we have an instruction that computes a pointer to an
        // array element and a dependent instruction that loads the value of
        // the pointee, like so:
        //
        //
        //                 |                       block 1                        |
        //                 | ==================================================== |
        //                 |                         ...                          |
        //                 | ptr = intrinsic(array.get_element_pointer, ...)(...) |
        //                 |                         ...                          |
        //                 | ==================================================== |
        //                            /                               \
        //                           /                                 \
        //                          /                                   \
        //                         /                                     \
        //                       ...                                 |       block 2        |
        //                                                           | ==================== |
        //                                                           |          ...         |
        //                                                           | val = load(...)(ptr) |
        //                                                           |          ...         |
        //                                                           | ==================== |
        //
        //
        // The first instruction performs a bounds check that may throw, but `ptr` from the
        // example above is only used if a particular branch is taken. So there's a real chance
        // that we perform the bounds check and never use `ptr` at all. That's bad for two reasons:
        //
        //   1. We wasted time on a bound check that didn't protect us from real danger.
        //      Out-of-bounds pointers are only dangerous if they are dereferenced.
        //
        //   2. Instruction sets like CIL contain specialized opcodes for loading elements from
        //      arrays (essentially a fused `array.get_element_pointer` + `load`).
        //
        // So what we'd like to do is replace the `load` with an `array.load_element` intrinsic
        // and delete the `array.get_element_pointer` intrinsic. Unfortunately, that transformation
        // changes the semantics of the program because it delays the point at which an exception
        // is thrown. Implementations of `ExceptionDelayability` tell us when it is okay to change
        // the semantics of a program like that.
    }

    /// <summary>
    /// Defines extension methods related to exception delayability.
    /// </summary>
    public static class ExceptionDelayabilityExtensions
    {
        /// <summary>
        /// Tells if it is permissible to delay exceptions thrown by a
        /// particular instruction until the instruction's result is used
        /// by an effectful instruction.
        /// If the instruction's result is never used that way,
        /// the exception may even be deleted altogether.
        /// </summary>
        /// <param name="graph">
        /// The flow graph that defines the instruction.
        /// </param>
        /// <param name="instruction">
        /// An instruction tag to examine.
        /// </param>
        /// <returns>
        /// <c>true</c> if exceptions thrown by <paramref name="instruction"/>
        /// may be delayed until its value is used by an effectful instruction;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool CanDelayExceptions(this FlowGraph graph, ValueTag instruction)
        {
            ExceptionDelayability delayability;
            if (graph.TryGetAnalysisResult(out delayability))
            {
                return delayability.CanDelayExceptions(
                    graph.GetInstruction(instruction).Prototype);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tells if it is permissible to delay exceptions thrown by a
        /// particular instruction until the instruction's result is used
        /// by an effectful instruction.
        /// If the instruction's result is never used that way,
        /// the exception may even be deleted altogether.
        /// </summary>
        /// <param name="graph">
        /// The flow graph that defines the instruction.
        /// </param>
        /// <param name="instruction">
        /// An instruction tag to examine.
        /// </param>
        /// <returns>
        /// <c>true</c> if exceptions thrown by <paramref name="instruction"/>
        /// may be delayed until its value is used by an effectful instruction;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool CanDelayExceptions(this FlowGraphBuilder graph, ValueTag instruction)
        {
            return graph.ImmutableGraph.CanDelayExceptions(instruction);
        }
    }

    /// <summary>
    /// Exception delayability rules that disallow delaying exceptions in
    /// all cases.
    /// </summary>
    public sealed class StrictExceptionDelayability : ExceptionDelayability
    {
        private StrictExceptionDelayability()
        { }

        /// <summary>
        /// An instance of the strict exception delayability policy.
        /// </summary>
        public static readonly StrictExceptionDelayability Instance =
            new StrictExceptionDelayability();

        /// <inheritdoc/>
        public override bool CanDelayExceptions(InstructionPrototype prototype)
        {
            return false;
        }
    }

    /// <summary>
    /// Exception delayability rules that allow delaying exceptions for
    /// implicit checks when computing pointers.
    /// </summary>
    public sealed class PermissiveExceptionDelayability : ExceptionDelayability
    {
        private PermissiveExceptionDelayability()
        { }

        /// <summary>
        /// An instance of the permissive exception delayability policy.
        /// </summary>
        public static readonly PermissiveExceptionDelayability Instance =
            new PermissiveExceptionDelayability();

        /// <inheritdoc/>
        public override bool CanDelayExceptions(InstructionPrototype prototype)
        {
            if (prototype is UnboxPrototype
                || prototype is GetFieldPointerPrototype
                || prototype is NewDelegatePrototype)
            {
                return true;
            }
            else if (prototype is IntrinsicPrototype)
            {
                var intrinsicProto = (IntrinsicPrototype)prototype;
                return delayableIntrinsics.Contains(intrinsicProto.Name);
            }
            else
            {
                return false;
            }
        }

        private static readonly HashSet<string> delayableIntrinsics =
            new HashSet<string>()
        {
            ArrayIntrinsics.Namespace.GetIntrinsicName(ArrayIntrinsics.Operators.GetElementPointer)
        };
    }
}
