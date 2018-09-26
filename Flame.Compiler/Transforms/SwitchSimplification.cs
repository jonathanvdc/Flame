using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;

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
                    var simpleFlow = SimplifySwitchFlow(flow, graphBuilder);
                    if (flow != simpleFlow)
                    {
                        block.Flow = simpleFlow;
                    }
                }
            }
            return graphBuilder.ToImmutable();
        }

        private static BlockFlow SimplifySwitchFlow(SwitchFlow flow, FlowGraphBuilder graph)
        {
            var value = SimplifyInstruction(flow.SwitchValue, graph);
            if (value.Prototype is ConstantPrototype)
            {
                // Turn the switch into a jump.
                var constant = ((ConstantPrototype)value.Prototype).Value;
                var valuesToBranches = flow.ValueToBranchMap;
                return new JumpFlow(
                    valuesToBranches.ContainsKey(constant)
                    ? valuesToBranches[constant]
                    : flow.DefaultBranch);
            }
            else if (ArithmeticIntrinsics.IsArithmeticIntrinsicPrototype(value.Prototype))
            {
                var proto = (IntrinsicPrototype)value.Prototype;
                var intrinsicName = ArithmeticIntrinsics.ParseArithmeticIntrinsicName(proto.Name);
                if (intrinsicName == ArithmeticIntrinsics.Operators.Convert
                    && proto.ParameterCount == 1
                    && flow.IsIntegerSwitch)
                {
                    // We can eliminate instructions that extend integers
                    // by changing the values in the list of cases.
                    var operand = proto.GetArgumentList(value).Single();
                    var operandType = graph.GetValueType(operand);
                    var convType = proto.ResultType;
                    var operandSpec = operandType.GetIntegerSpecOrNull();
                    var convSpec = convType.GetIntegerSpecOrNull();

                    if (operandSpec.Size > convSpec.Size)
                    {
                        // We can't handle this case. To handle it anyway
                        // would require us to introduce additional cases
                        // and that's costly.
                        return flow;
                    }

                    var caseList = new List<SwitchCase>();
                    foreach (var switchCase in flow.Cases)
                    {
                        // Retain only those switch cases that have values
                        // that are in the range of the conversion function.
                        var values = ImmutableHashSet.CreateBuilder<Constant>();
                        foreach (var val in switchCase.Values.Cast<IntegerConstant>())
                        {
                            if (val.Cast(operandSpec).Cast(convSpec).Equals(val))
                            {
                                values.Add(val);
                            }
                        }
                        if (values.Count > 0)
                        {
                            caseList.Add(new SwitchCase(values.ToImmutableHashSet(), switchCase.Branch));
                        }
                    }
                    return SimplifySwitchFlow(
                        new SwitchFlow(
                            Instruction.CreateCopy(
                                operandType,
                                operand),
                            caseList,
                            flow.DefaultBranch),
                        graph);
                }
            }
            return flow;
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
