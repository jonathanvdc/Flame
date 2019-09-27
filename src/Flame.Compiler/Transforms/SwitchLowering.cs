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
    /// A switch lowering transform, which rewrites general switch
    /// flow as if-else switch flow and jump table switch flow.
    /// </summary>
    public sealed class SwitchLowering : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates a switch lowering transform.
        /// </summary>
        /// <param name="typeEnvironment">
        /// The type environment to use.
        /// </param>
        /// <param name="allowBitTests">
        /// Tells if it is permissible to generate bit tests.
        /// </param>
        /// <param name="allowJumpTables">
        /// Tells if it is permissible to generate jump tables.
        /// </param>
        public SwitchLowering(
            TypeEnvironment typeEnvironment,
            bool allowBitTests = true,
            bool allowJumpTables = true)
        {
            this.TypeEnvironment = typeEnvironment;
            this.AllowBitTests = allowBitTests;
            this.AllowJumpTables = allowJumpTables;
        }

        /// <summary>
        /// Gets the type environment used by this switch lowering pass.
        /// </summary>
        /// <value>The type environment.</value>
        public TypeEnvironment TypeEnvironment { get; private set; }

        /// <summary>
        /// Tells if it is permissible to generate bit tests.
        /// </summary>
        /// <value>
        /// <c>true</c> if bit tests may be generated; otherwise, <c>false</c>.
        /// </value>
        public bool AllowBitTests { get; private set; }

        /// <summary>
        /// Tells if it is permissible to generate jump tables.
        /// </summary>
        /// <value>
        /// <c>true</c> if jump tables may be generated; otherwise, <c>false</c>.
        /// </value>
        public bool AllowJumpTables { get; private set; }

        /// <summary>
        /// Lowers general switch flow in a particular flow graph.
        /// </summary>
        /// <param name="graph">The flow graph to rewrite.</param>
        /// <returns>A rewritten flow graph.</returns>
        public override FlowGraph Apply(FlowGraph graph)
        {
            var graphBuilder = graph.ToBuilder();
            foreach (var block in graphBuilder.BasicBlocks)
            {
                var flow = block.Flow;
                if (flow is SwitchFlow)
                {
                    var switchFlow = (SwitchFlow)flow;
                    if (switchFlow.IsIfElseFlow)
                    {
                        // If-else flow is its own optimal lowering.
                        continue;
                    }
                    var value = block.AppendInstruction(switchFlow.SwitchValue, "switchval");
                    block.Flow = GetSwitchFlowLowering(switchFlow)
                        .Emit(graphBuilder, value);
                }
            }
            return graphBuilder.ToImmutable();
        }

        private SwitchFlowLowering GetSwitchFlowLowering(SwitchFlow flow)
        {
            // This algorithm is based on the switch-lowering algorithm described in
            // "Improving Switch Lowering for The LLVM Compiler System" by Anton Korobeynikov
            // (http://llvm.org/pubs/2007-05-31-Switch-Lowering.pdf)

            if (flow.Cases.Count == 0)
            {
                return new JumpLowering(flow.DefaultBranch);
            }
            else if (flow.IsIntegerSwitch)
            {
                var values = flow.Cases
                    .SelectMany(item => item.Values)
                    .Cast<IntegerConstant>()
                    .OrderBy(val => val)
                    .ToArray();

                var minValue = values[0];
                var maxValue = values[values.Length - 1];
                var valueRange = maxValue.Subtract(minValue);

                if (ShouldUseBitTestSwitch(valueRange, flow.Cases.Count, values.Length))
                {
                    return new BitTestLowering(flow, minValue, valueRange, TypeEnvironment);
                }
                else if (values.Length <= 3)
                {
                    return new TestCascadeLowering(flow);
                }
                else if (ShouldUseJumpTable(valueRange, values.Length))
                {
                    return new JumpTableLowering(flow);
                }
                else
                {
                    var splitFlows = SplitIntegerSwitch(values, flow);
                    return new SearchTreeLowering(
                        splitFlows.Item1,
                        GetSwitchFlowLowering(splitFlows.Item2),
                        GetSwitchFlowLowering(splitFlows.Item3),
                        TypeEnvironment);
                }
            }
            else
            {
                // TODO: use the search tree lowering for large switches
                // with non-integer, comparable constants (e.g., float32
                // and float64).
                return new TestCascadeLowering(flow);
            }
        }

        /// <summary>
        /// Splits an integer switch into two switches.
        /// </summary>
        /// <param name="values">
        /// A sorted list of all values the switch flow lists in its cases.
        /// </param>
        /// <param name="flow">The switch flow to split.</param>
        /// <returns>A (pivot value, left switch, right switch) triple.</returns>
        private Tuple<IntegerConstant, SwitchFlow, SwitchFlow> SplitIntegerSwitch(
            IntegerConstant[] values,
            SwitchFlow flow)
        {
            int pivotIndex = ChooseIntegerPivotIndex(values);
            var leftValues = ImmutableHashSet.CreateRange(
                new ArraySegment<IntegerConstant>(
                    values,
                    0,
                    pivotIndex + 1));

            var rightValues = ImmutableHashSet.CreateRange(
                new ArraySegment<IntegerConstant>(
                    values,
                    pivotIndex + 1,
                    values.Length - pivotIndex - 1));

            var leftCases = new List<SwitchCase>();
            var rightCases = new List<SwitchCase>();
            foreach (var switchCase in flow.Cases)
            {
                var leftPattern = switchCase.Values.Intersect(leftValues);
                if (!leftPattern.IsEmpty)
                {
                    leftCases.Add(new SwitchCase(leftPattern, switchCase.Branch));
                }

                var rightPattern = switchCase.Values.Intersect(rightValues);
                if (!rightPattern.IsEmpty)
                {
                    rightCases.Add(new SwitchCase(rightPattern, switchCase.Branch));
                }
            }

            return new Tuple<IntegerConstant, SwitchFlow, SwitchFlow>(
                values[pivotIndex],
                new SwitchFlow(flow.SwitchValue, leftCases, flow.DefaultBranch),
                new SwitchFlow(flow.SwitchValue, rightCases, flow.DefaultBranch));
        }

        /// <summary>
        /// Chooses an optimal pivot at which to split an integer switch flow.
        /// </summary>
        /// <param name="values">
        /// A sorted list of all values the switch flow lists in its cases.
        /// </param>
        /// <returns>
        /// The index of the pivot in <paramref name="values"/>.
        /// </returns>
        private int ChooseIntegerPivotIndex(IntegerConstant[] values)
        {
            // The pivot score that is computed here is borrowed from
            // "Improving Switch Lowering for The LLVM Compiler System" by Anton Korobeynikov
            // (http://llvm.org/pubs/2007-05-31-Switch-Lowering.pdf)

            int pivotIndex = 0;
            double bestPivotScore = 0;

            var minValue = values[0];
            var maxValue = values[values.Length - 1];
            for (int i = 1; i < values.Length - 1; i++)
            {
                var leftRange = values[i].Subtract(minValue).ToFloat64() + 1;
                var leftDensity = (i + 1) / leftRange;
                var rightRange = maxValue.Subtract(values[i + 1]).ToFloat64() + 1;
                var rightDensity = (values.Length - i - 1) / rightRange;
                var gap = values[i + 1].Subtract(values[i]).ToFloat64();
                var score = (leftDensity + rightDensity) * Math.Log(gap);
                if (score > bestPivotScore)
                {
                    pivotIndex = i;
                    bestPivotScore = score;
                }
            }

            return pivotIndex;
        }

        /// <summary>
        /// Tells if it is both possible and sensible to use bit tests to implement
        /// a particular switch.
        /// </summary>
        /// <param name="valueRange">
        /// The difference between the largest and smallest value in the switch flow.
        /// </param>
        /// <param name="caseCount">
        /// The number of cases in the switch flow.
        /// </param>
        /// <param name="valueCount">
        /// The total number of values in the switch flow.
        /// </param>
        /// <returns><c>true</c> if bit tests should be used; otherwise, <c>false</c>.</returns>
        private bool ShouldUseBitTestSwitch(IntegerConstant valueRange, int caseCount, int valueCount)
        {
            if (!AllowBitTests)
            {
                // We may not be allowed to use bit tests.
                return false;
            }

            // If the span's range is at least 64 then we can't use a bit mask.
            if (valueRange.IsGreaterThan(new IntegerConstant(64)))
            {
                return false;
            }

            // We know that we can use bit tests for this range and we can now
            // decide if it's actually a good idea to do so.
            return (caseCount == 1 && valueCount >= 3)
                || (caseCount == 2 && valueCount >= 5)
                || (caseCount == 3 && valueCount >= 6);
        }

        /// <summary>
        /// Tells if it is sensible to use a jump table to implement a
        /// particular integer switch.
        /// </summary>
        /// <param name="valueRange">
        /// The difference between the max and min values to switch on.
        /// </param>
        /// <param name="valueCount">
        /// The number of values to switch on.
        /// </param>
        /// <returns>
        /// <c>true</c> if a jump table should be used; otherwise, <c>false</c>.
        /// </returns>
        private bool ShouldUseJumpTable(IntegerConstant valueRange, int valueCount)
        {
            if (!AllowJumpTables)
            {
                // We may not be allowed to use jump tables.
                return false;
            }

            double density = valueCount / (valueRange.ToFloat64() + 1);
            return density >= 0.4;
        }

        /// <summary>
        /// A particular way to lower a switch flow.
        /// </summary>
        private abstract class SwitchFlowLowering
        {
            /// <summary>
            /// Turns this switch flow lowering into an
            /// actual block flow.
            /// </summary>
            /// <param name="graph">A flow graph builder.</param>
            /// <param name="value">
            /// The value being switched on.
            /// </param>
            /// <returns>Block flow.</returns>
            public abstract BlockFlow Emit(
                FlowGraphBuilder graph,
                ValueTag value);
        }

        /// <summary>
        /// A switch flow lowering that redirects flow to a particular branch.
        /// </summary>
        private sealed class JumpLowering : SwitchFlowLowering
        {
            public JumpLowering(Branch branch)
            {
                this.Branch = branch;
            }

            /// <summary>
            /// Gets the branch performed by this switch flow lowering.
            /// </summary>
            /// <value>A branch.</value>
            public Branch Branch { get; private set; }

            /// <inheritdoc/>
            public override BlockFlow Emit(
                FlowGraphBuilder graph,
                ValueTag value)
            {
                return new JumpFlow(Branch);
            }
        }

        private sealed class BitTestLowering : SwitchFlowLowering
        {
            public BitTestLowering(
                SwitchFlow flow,
                IntegerConstant minValue,
                IntegerConstant valueRange,
                TypeEnvironment typeEnvironment)
            {
                this.Flow = flow;
                this.MinValue = minValue;
                this.ValueRange = valueRange;
                this.TypeEnvironment = typeEnvironment;
            }

            public SwitchFlow Flow { get; private set; }

            public IntegerConstant MinValue { get; private set; }

            public IntegerConstant ValueRange { get; private set; }

            public TypeEnvironment TypeEnvironment { get; private set; }

            public override BlockFlow Emit(
                FlowGraphBuilder graph,
                ValueTag value)
            {
                // Create the following blocks:
                //
                // bitswitch.entry():
                //   minvalue = const <MinValue>
                //   switchval.adjusted = switchval - minvalue
                //   switchval.unsigned = (uintX)switchval.adjusted
                //   valrange = const <ValueRange>
                //   switch (switchval.unsigned > valrange)
                //     0 -> bitswitch.header()
                //     default -> <defaultBranch>
                //
                // bitswitch.header():
                //   one = const 1
                //   shifted = one << switchval.unsigned
                //   bitmask1 = const <bitmask1>
                //   switch (shifted & bitmask1)
                //     0 -> bitswitch.case2()
                //     default -> <case1Branch>
                //
                // bitswitch.case2():
                //   bitmask2 = const <bitmask2>
                //   switch (shifted & bitmask1)
                //     0 -> bitswitch.case3()
                //     default -> <case2Branch>
                //
                // ...

                var entryBlock = graph.AddBasicBlock("bitswitch.entry");
                var headerBlock = graph.AddBasicBlock("bitswitch.header");

                var valueType = graph.GetValueType(value);
                var valueSpec = valueType.GetIntegerSpecOrNull();

                var defaultBranch = Flow.DefaultBranch;

                // Subtract the min value from the switch value if necessary.
                if (!MinValue.IsZero)
                {
                    value = entryBlock.AppendInstruction(
                        Instruction.CreateBinaryArithmeticIntrinsic(
                            ArithmeticIntrinsics.Operators.Subtract,
                            false,
                            valueType,
                            value,
                            entryBlock.AppendInstruction(
                                Instruction.CreateConstant(MinValue, valueType),
                                "minvalue")),
                        "switchval.adjusted");
                }

                // Make the switch value unsigned if it wasn't already.
                if (valueSpec.IsSigned)
                {
                    var uintType = TypeEnvironment.MakeUnsignedIntegerType(valueSpec.Size);
                    value = entryBlock.AppendInstruction(
                        Instruction.CreateConvertIntrinsic(
                            false,
                            uintType,
                            valueType,
                            value),
                        "switchval.unsigned");
                    valueType = uintType;
                    valueSpec = uintType.GetIntegerSpecOrNull();
                }

                // Check that the value is within range.
                entryBlock.Flow = SwitchFlow.CreateIfElse(
                    Instruction.CreateRelationalIntrinsic(
                        ArithmeticIntrinsics.Operators.IsGreaterThan,
                        TypeEnvironment.Boolean,
                        valueType,
                        value,
                        entryBlock.AppendInstruction(
                            Instruction.CreateConstant(
                                ValueRange.CastSignedness(false),
                                valueType),
                            "valrange")),
                    defaultBranch,
                    new Branch(headerBlock));

                // Pick an appropriate type for the bitmasks.
                var bitmaskType = valueType;
                if (valueSpec.Size < 32)
                {
                    bitmaskType = TypeEnvironment.UInt32;
                }
                if (ValueRange.IsGreaterThan(new IntegerConstant(valueSpec.Size, ValueRange.Spec)))
                {
                    bitmaskType = TypeEnvironment.UInt64;
                }

                // Set up first part of the header block.
                if (bitmaskType != valueType)
                {
                    valueSpec = bitmaskType.GetIntegerSpecOrNull();
                }

                var zero = headerBlock.AppendInstruction(
                    Instruction.CreateConstant(
                        new IntegerConstant(0, valueSpec),
                        valueType),
                    "zero");

                var one = headerBlock.AppendInstruction(
                    Instruction.CreateConstant(
                        new IntegerConstant(1, valueSpec),
                        valueType),
                    "one");

                value = headerBlock.AppendInstruction(
                    Instruction.CreateArithmeticIntrinsic(
                        ArithmeticIntrinsics.Operators.LeftShift,
                        false,
                        bitmaskType,
                        new[] { bitmaskType, valueType },
                        new[] { one, value }),
                    "shifted");

                valueType = bitmaskType;

                // Start emitting cases.
                var caseBlock = headerBlock;
                var nextCase = graph.AddBasicBlock("bitswitch.case1");
                for (int i = 0; i < Flow.Cases.Count; i++)
                {
                    // Construct a mask for the case.
                    var switchCase = Flow.Cases[i];
                    var oneConstant = new IntegerConstant(1, valueSpec);
                    var mask = new IntegerConstant(0, valueSpec);
                    foreach (var pattern in switchCase.Values)
                    {
                        mask = mask.BitwiseOr(oneConstant.ShiftLeft(((IntegerConstant)pattern).Subtract(MinValue)));
                    }

                    // Switch on the bitwise 'and' of the mask and
                    // the shifted value.
                    caseBlock.Flow = SwitchFlow.CreateIfElse(
                        Instruction.CreateBinaryArithmeticIntrinsic(
                            ArithmeticIntrinsics.Operators.And,
                            false,
                            valueType,
                            value,
                            caseBlock.AppendInstruction(
                                Instruction.CreateConstant(mask, valueType),
                                "bitmask" + i)),
                        switchCase.Branch,
                        new Branch(nextCase));

                    caseBlock = nextCase;
                    nextCase = graph.AddBasicBlock("bitswitch.case" + (i + 2));
                }

                // Jump to the default branch if nothing matches.
                caseBlock.Flow = new JumpFlow(defaultBranch);

                // Jump to the header block and let it do all of the heavy
                // lifting.
                return new JumpFlow(entryBlock);
            }
        }

        /// <summary>
        /// A switch lowering that defers to one of two switch
        /// lowerings depending on whether the switch value is
        /// greater than a pivot value or not.
        /// </summary>
        private sealed class SearchTreeLowering : SwitchFlowLowering
        {
            public SearchTreeLowering(
                Constant pivot,
                SwitchFlowLowering leftTree,
                SwitchFlowLowering rightTree,
                TypeEnvironment typeEnvironment)
            {
                this.pivot = pivot;
                this.leftTree = leftTree;
                this.rightTree = rightTree;
                this.typeEnvironment = typeEnvironment;
            }

            private Constant pivot;
            private SwitchFlowLowering leftTree;
            private SwitchFlowLowering rightTree;
            private TypeEnvironment typeEnvironment;

            /// <inheritdoc/>
            public override BlockFlow Emit(FlowGraphBuilder graph, ValueTag value)
            {
                var headerBlock = graph.AddBasicBlock("searchtree.header");
                var leftBlock = graph.AddBasicBlock("searchtree.left");
                var rightBlock = graph.AddBasicBlock("searchtree.right");

                var valueType = graph.GetValueType(value);

                headerBlock.Flow = SwitchFlow.CreateIfElse(
                    Instruction.CreateRelationalIntrinsic(
                        ArithmeticIntrinsics.Operators.IsGreaterThan,
                        typeEnvironment.Boolean,
                        valueType,
                        value,
                        headerBlock.AppendInstruction(
                            Instruction.CreateConstant(pivot, valueType))),
                    new Branch(rightBlock),
                    new Branch(leftBlock));

                leftBlock.Flow = leftTree.Emit(graph, value);
                rightBlock.Flow = rightTree.Emit(graph, value);

                return new JumpFlow(headerBlock);
            }
        }

        /// <summary>
        /// A switch lowering that implements a switch as a cascade of
        /// if-else tests.
        /// </summary>
        private class TestCascadeLowering : SwitchFlowLowering
        {
            public TestCascadeLowering(SwitchFlow flow)
            {
                this.flow = flow;
            }

            private SwitchFlow flow;

            /// <inheritdoc/>
            public override BlockFlow Emit(FlowGraphBuilder graph, ValueTag value)
            {
                int caseCounter = 0;
                BasicBlockBuilder currentBlock = null;
                var defaultJump = new JumpFlow(flow.DefaultBranch);
                BlockFlow result = defaultJump;
                foreach (var switchCase in flow.Cases)
                {
                    foreach (var pattern in switchCase.Values)
                    {
                        caseCounter++;
                        var nextBlock = graph.AddBasicBlock("case" + caseCounter);
                        var blockFlow = new SwitchFlow(
                            Instruction.CreateCopy(graph.GetValueType(value), value),
                            new[]
                            {
                                new SwitchCase(ImmutableHashSet.Create(pattern), switchCase.Branch)
                            },
                            new Branch(nextBlock));

                        if (currentBlock == null)
                        {
                            result = blockFlow;
                        }
                        else
                        {
                            currentBlock.Flow = blockFlow;
                        }
                        currentBlock = nextBlock;
                    }
                }

                if (currentBlock != null)
                {
                    currentBlock.Flow = defaultJump;
                }

                return result;
            }
        }

        /// <summary>
        /// An integer switch lowering that builds jump tables.
        /// </summary>
        private sealed class JumpTableLowering : SwitchFlowLowering
        {
            public JumpTableLowering(SwitchFlow flow)
            {
                this.flow = flow;
            }

            private SwitchFlow flow;

            public override BlockFlow Emit(FlowGraphBuilder graph, ValueTag value)
            {
                var cases = new List<SwitchCase>();
                int thunkCount = 0;
                foreach (var switchCase in flow.Cases)
                {
                    if (switchCase.Branch.Arguments.Count > 0)
                    {
                        // Jump tables can't have branch arguments. Work
                        // around that limitation by jumping to a thunk
                        // block.
                        var thunk = graph.AddBasicBlock("jumptable.thunk" + thunkCount);
                        thunkCount++;
                        cases.Add(new SwitchCase(switchCase.Values, new Branch(thunk)));
                        thunk.Flow = new JumpFlow(switchCase.Branch);
                    }
                    else
                    {
                        cases.Add(switchCase);
                    }
                }
                return new SwitchFlow(
                    Instruction.CreateCopy(graph.GetValueType(value), value),
                    cases,
                    flow.DefaultBranch);
            }
        }
    }
}
