using System.Collections.Generic;
using System.Threading.Tasks;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Compiler.Pipeline;

namespace Flame.Compiler.Transforms
{
    // There are two distinct "flavors" of tail recursion.
    //
    //     1. Regular tail recursion, which is eliminated by replacing
    //        recursive tail calls with jumps to the function's entry
    //        point. Eliminating this type of tail recursion is pretty
    //        easy: just replace the return flow with a jump to the
    //        entry point.
    //
    //     2. Tail recursion modulo cons. This flavor of tail recursion
    //        also allows for an operator to be applied to the return value
    //        of a recursive call, provided that the operator induces a 
    //        commutative monoid (i.e., it's an associative and commutative
    //        operator with a neutral element).
    //
    //        Tail recursion modulo cons can be eliminated by introducing
    //        an accumulator variable, like so: 
    //
    //            f(x, y, z)
    //            {
    //                if (x == 0)
    //                    return g();
    //                else
    //                    return z + f(x - 1, y, z);
    //            }
    //
    //            ==>
    //
    //            f(x, y, z)
    //            {
    //                acc = 0;
    //            entry:
    //                if (x == 0)
    //                {
    //                    acc += g();
    //                    return acc;
    //                }
    //                else
    //                {
    //                    acc += z;
    //                    x = x - 1;
    //                    goto entry;
    //                }
    //            }
    //
    // At the moment, this tail recursion pass implements the first type of
    // tail recursion only; tail recursion modulo cons cannot be eliminated yet.

    /// <summary>
    /// A transform that rewrites static calls to the current method
    /// just prior to a return as unconditional jumps to the entry point.
    /// </summary>
    public sealed class TailRecursionElimination : Optimization
    {
        private TailRecursionElimination()
        { }

        /// <summary>
        /// An instance of the tail recursion elimination optimization.
        /// </summary>
        /// <value>A tail recursion elimination optimization.</value>
        public static readonly TailRecursionElimination Instance
            = new TailRecursionElimination();

        /// <inheritdoc/>
        public override bool IsCheckpoint => false;

        /// <inheritdoc/>
        public override Task<MethodBody> ApplyAsync(MethodBody body, OptimizationState state)
        {
            return Task.FromResult(
                body.WithImplementation(
                    Apply(body.Implementation, state.Method)));
        }

        /// <summary>
        /// Applies the tail recursion elimination transform to a particular method.
        /// </summary>
        /// <param name="graph">
        /// A control-flow graph to optimize.
        /// </param>
        /// <param name="method">
        /// The method that has <paramref name="graph"/> as its body.
        /// </param>
        /// <returns>An optimized control-flow graph.</returns>
        public FlowGraph Apply(FlowGraph graph, IMethod method)
        {
            var builder = graph.ToBuilder();
            var ordering = graph.GetAnalysisResult<InstructionOrdering>();
            foreach (var block in builder.BasicBlocks)
            {
                if (block.Flow is ReturnFlow)
                {
                    var retFlow = (ReturnFlow)block.Flow;
                    TryReplaceWithBranchToEntry(
                        block,
                        retFlow.ReturnValue,
                        builder,
                        method,
                        ordering);
                }
            }
            return builder.ToImmutable();
        }

        /// <summary>
        /// Tries to replace a return flow with a branch to
        /// the entry point.
        /// </summary>
        /// <param name="block">
        /// A block that ends in return flow.
        /// </param>
        /// <param name="returnValue">
        /// The value returned by the return flow.
        /// </param>
        /// <param name="graph">
        /// The graph that defines the block.
        /// </param>
        /// <param name="method">
        /// The method that is being optimized.
        /// </param>
        /// <param name="ordering">
        /// An instruction ordering model.
        /// </param>
        /// <returns>
        /// <c>true</c> if the return has been replaced with a branch
        /// to the entry point by this method; otherwise, <c>false</c>.
        /// </returns>
        private bool TryReplaceWithBranchToEntry(
            BasicBlockBuilder block,
            Instruction returnValue,
            FlowGraphBuilder graph,
            IMethod method,
            InstructionOrdering ordering)
        {
            var insnsToEliminate = new HashSet<ValueTag>();
            ValueTag callTag = null;

            // Jump through copy instructions.
            while (returnValue.Prototype is CopyPrototype)
            {
                callTag = ((CopyPrototype)returnValue.Prototype).GetCopiedValue(returnValue);
                insnsToEliminate.Add(callTag);
                if (!graph.ContainsInstruction(callTag))
                {
                    // We encountered a basic block parameter, which we can't really
                    // peek through. Abort.
                    return false;
                }
                returnValue = graph.GetInstruction(callTag).Instruction;
            }

            if ((callTag == null || graph.GetValueParent(callTag).Tag == block.Tag)
                && IsSelfCallPrototype(returnValue.Prototype, method))
            {
                // Now all we have to do is make sure that there is no instruction
                // that must run after the call.
                if (callTag != null)
                {
                    var selection = graph.GetInstruction(callTag).NextInstructionOrNull;
                    while (selection != null)
                    {
                        if (ordering.MustRunBefore(callTag, selection))
                        {
                            // Aw, snap. Looks like we can't reorder the call, which
                            // means no tail call elimination. Abort.
                            return false;
                        }
                        selection = selection.NextInstructionOrNull;
                    }
                }

                // Remove some instructions.
                foreach (var tag in insnsToEliminate)
                {
                    graph.RemoveInstruction(tag);
                }

                // Turn the return flow into a jump.
                block.Flow = new JumpFlow(graph.EntryPointTag, returnValue.Arguments);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tells if a particular instruction prototype is a recursive call.
        /// </summary>
        /// <param name="prototype">The instruction prototype to inspect.</param>
        /// <param name="method">The method that defines the current CFG being optimized.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="prototype"/> describes a recursive call;
        /// otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSelfCallPrototype(
            InstructionPrototype prototype,
            IMethod method)
        {
            if (prototype is CallPrototype)
            {
                var callProto = (CallPrototype)prototype;
                return callProto.Lookup == MethodLookup.Static
                    && callProto.Callee.Equals(method);
            }
            else
            {
                return false;
            }
        }
    }
}
