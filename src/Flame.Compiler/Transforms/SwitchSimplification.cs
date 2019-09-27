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
                TrySimplifySwitchFlow(block);
            }
            return graphBuilder.ToImmutable();
        }

        /// <summary>
        /// Tries to simplify a basic block's switch flow, provided that the
        /// block ends in switch flow.
        /// </summary>
        /// <param name="block">The basic block to simplify.</param>
        /// <returns>
        /// <c>true</c> if the block's flow was simplified; otherwise, <c>false</c>.
        /// </returns>
        public static bool TrySimplifySwitchFlow(BasicBlockBuilder block)
        {
            if (block.Flow is SwitchFlow)
            {
                var flow = (SwitchFlow)block.Flow;
                var simpleFlow = SimplifySwitchFlow(flow, block.Graph.ImmutableGraph);
                if (flow != simpleFlow)
                {
                    if (simpleFlow is JumpFlow)
                    {
                        // If a switch flow gets simplified to a jump
                        // flow, then we should still ensure that the
                        // switch value is evaluated.
                        block.AppendInstruction(flow.SwitchValue);
                    }
                    block.Flow = simpleFlow;
                    return true;
                }
            }
            return false;
        }

        private static BlockFlow SimplifySwitchFlow(SwitchFlow flow, FlowGraph graph)
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

                    if (operandSpec == null)
                    {
                        // The operand of the conversion intrinsic is not an
                        // integer.
                        return flow;
                    }

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
                            var opVal = val.Cast(operandSpec);
                            if (opVal.Cast(convSpec).Equals(val))
                            {
                                values.Add(opVal);
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
                else if (intrinsicName == ArithmeticIntrinsics.Operators.IsEqualTo
                    && proto.ParameterCount == 2
                    && proto.ResultType.IsIntegerType())
                {
                    var args = proto.GetArgumentList(value);
                    var lhs = args[0];
                    var rhs = args[1];
                    Constant constant;
                    ValueTag operand;
                    if (TryExtractConstantAndValue(lhs, rhs, graph, out constant, out operand))
                    {
                        // The 'arith.eq' intrinsic always either produces '0' or '1'.
                        // Because of that property, we can safely rewrite switches
                        // like so:
                        //
                        // switch arith.eq(value, constant)
                        //   0 -> zeroBranch
                        //   1 -> oneBranch
                        //   default -> defaultBranch
                        //
                        // -->
                        //
                        // switch value
                        //   constant -> oneBranch ?? defaultBranch
                        //   default -> zeroBranch ?? defaultBranch
                        //
                        var resultSpec = proto.ResultType.GetIntegerSpecOrNull();
                        var zeroVal = new IntegerConstant(0, resultSpec);
                        var oneVal = new IntegerConstant(1, resultSpec);

                        var valuesToBranches = flow.ValueToBranchMap;
                        var zeroBranch = valuesToBranches.ContainsKey(zeroVal)
                            ? valuesToBranches[zeroVal]
                            : flow.DefaultBranch;
                        var oneBranch = valuesToBranches.ContainsKey(oneVal)
                            ? valuesToBranches[oneVal]
                            : flow.DefaultBranch;

                        return SimplifySwitchFlow(
                            new SwitchFlow(
                                Instruction.CreateCopy(
                                    graph.GetValueType(operand),
                                    operand),
                                new[] { new SwitchCase(ImmutableHashSet.Create(constant), oneBranch) },
                                zeroBranch),
                            graph);
                    }
                }
            }
            return flow;
        }

        private static bool TryExtractConstantAndValue(
            ValueTag leftHandSide,
            ValueTag rightHandSide,
            FlowGraph graph,
            out Constant constant,
            out ValueTag value)
        {
            var lhsInstruction = SimplifyInstruction(
                Instruction.CreateCopy(
                    graph.GetValueType(leftHandSide),
                    leftHandSide),
                graph);

            if (lhsInstruction.Prototype is ConstantPrototype)
            {
                constant = ((ConstantPrototype)lhsInstruction.Prototype).Value;
                value = rightHandSide;
                return true;
            }

            var rhsInstruction = SimplifyInstruction(
                Instruction.CreateCopy(
                    graph.GetValueType(rightHandSide),
                    rightHandSide),
                graph);

            if (rhsInstruction.Prototype is ConstantPrototype)
            {
                constant = ((ConstantPrototype)rhsInstruction.Prototype).Value;
                value = leftHandSide;
                return true;
            }
            else
            {
                constant = null;
                value = null;
                return false;
            }
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
            FlowGraph graph)
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
