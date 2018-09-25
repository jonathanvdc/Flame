using System;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that transforms switch flows in a way that
    /// makes it easier for other optimizations to reason about said
    /// switches.
    /// </summary>
    public sealed class SwitchSimplification : IntraproceduralOptimization
    {
        private SwitchSimplification()
        { }

        /// <summary>
        /// An instance of the switch simplification optimization.
        /// </summary>
        public static readonly SwitchSimplification Instance = new SwitchSimplification();

        /// <summary>
        /// Simplifies switches in a particular flow graph.
        /// </summary>
        /// <param name="graph">The graph to transform.</param>
        /// <returns>A transformed graph.</returns>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var graphBuilder = graph.ToBuilder();
            foreach (var block in graphBuilder.BasicBlocks)
            {
                if (block.Flow is SwitchFlow)
                {
                    var flow = (SwitchFlow)block.Flow;
                    var value = SimplifyInstruction(flow.SwitchValue, graphBuilder);
                    if (value.Prototype is ConstantPrototype)
                    {
                        // Turn the switch into a jump.
                        var constant = ((ConstantPrototype)value.Prototype).Value;
                        var valuesToBranches = flow.ValueToBranchMap;
                        block.Flow = new JumpFlow(
                            valuesToBranches.ContainsKey(constant)
                            ? valuesToBranches[constant]
                            : flow.DefaultBranch);
                    }
                    else if (ArithmeticIntrinsics.IsArithmeticIntrinsicPrototype(value.Prototype))
                    {
                        var proto = (IntrinsicPrototype)value.Prototype;
                        throw new NotImplementedException();
                    }
                }
            }
            return graphBuilder.ToImmutable();
        }

        /// <summary>
        /// Looks for an instruction that is semantically
        /// equivalent to a given instruction but minimizes
        /// the number of layers of indirection erected by
        /// copies.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to simplify.
        /// </param>
        /// <param name="graph">
        /// The graph that defines the instruction.
        /// </param>
        /// <returns>
        /// A semantically equivalent instruction.</returns>
        private static Instruction SimplifyInstruction(
            Instruction instruction,
            FlowGraphBuilder graph)
        {
            if (instruction.Prototype is CopyPrototype)
            {
                var copyProto = (CopyPrototype)instruction.Prototype;
                var copiedVal = copyProto.GetCopiedValue(instruction);
                if (graph.ContainsInstruction(copiedVal))
                {
                    var copiedInsn = graph.GetInstruction(copiedVal).Instruction;
                    // Only simplify copies of arithmetic intriniscs and constants.
                    // Those are the only instructions we're actually
                    // interested in and they don't have any "funny" behavior.
                    if (copiedInsn.Prototype is ConstantPrototype
                        || copiedInsn.Prototype is CopyPrototype
                        || ArithmeticIntrinsics.IsArithmeticIntrinsicPrototype(copiedInsn.Prototype))
                    {
                        return SimplifyInstruction(copiedInsn, graph);
                    }
                }
            }
            return instruction;
        }
    }
}
