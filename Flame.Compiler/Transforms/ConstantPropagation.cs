using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// A transform that evaluates non-effectful instructions at
    /// compile-time and propagates their results. Essentially
    /// just an implementation of sparse conditional constant
    /// propagation.
    /// </summary>
    public sealed class ConstantPropagation : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates a constant propagation transform that uses the default
        /// evaluation function.
        /// </summary>
        /// <param name="evaluate">
        /// The evaluation function to use. It valuates an instruction that
        /// takes a list of constant arguments. It returns <c>null</c> if the
        /// instruction cannot be evaluated; otherwise, it returns the constant
        /// to which it was evaluated.
        /// </param>
        public ConstantPropagation()
            : this(EvaluateDefault)
        { }

        /// <summary>
        /// Creates a constant propagation transform that uses a particular
        /// evaluation function.
        /// </summary>
        /// <param name="evaluate">
        /// The evaluation function to use. It valuates an instruction that
        /// takes a list of constant arguments. It returns <c>null</c> if the
        /// instruction cannot be evaluated; otherwise, it returns the constant
        /// to which it was evaluated.
        /// </param>
        public ConstantPropagation(
            Func<InstructionPrototype, IReadOnlyList<Constant>, Constant> evaluate)
        {
            this.Evaluate = evaluate;
        }

        /// <summary>
        /// Evaluates an instruction that takes a list of constant arguments.
        /// Returns <c>null</c> if the instruction cannot be evaluated.
        /// </summary>
        public Func<InstructionPrototype, IReadOnlyList<Constant>, Constant> Evaluate { get; private set; }

        /// <summary>
        /// The default constant instruction evaluation function.
        /// </summary>
        /// <param name="prototype">
        /// The prorotype of the instruction to evaluate.
        /// </param>
        /// <param name="arguments">
        /// A list of arguments to the instruction, all of which
        /// must be constants.
        /// </param>
        /// <returns></returns>
        public static Constant EvaluateDefault(
            InstructionPrototype prototype,
            IReadOnlyList<Constant> arguments)
        {
            if (prototype is CopyPrototype
                || prototype is ReinterpretCastPrototype)
            {
                return arguments[0];
            }
            else if (prototype is ConstantPrototype)
            {
                var constProto = (ConstantPrototype)prototype;
                if (constProto.Value is DefaultConstant)
                {
                    // Try to specialize 'default' constants.
                    var intSpec = constProto.ResultType.GetIntegerSpecOrNull();
                    if (intSpec != null)
                    {
                        return new IntegerConstant(0, intSpec);
                    }
                }
                return constProto.Value;
            }
            else if (prototype is IntrinsicPrototype)
            {
                string arithOp;
                Constant result;

                if (ArithmeticIntrinsics.TryParseArithmeticIntrinsicName(
                    ((IntrinsicPrototype)prototype).Name,
                    out arithOp)
                    && ArithmeticIntrinsics.TryEvaluate(
                        arithOp,
                        prototype.ResultType,
                        arguments,
                        out result))
                {
                    return result;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            IEnumerable<BasicBlockTag> liveBlocks;

            // Do the fancy analysis.
            var cells = FillCells(graph, out liveBlocks);

            // Get ready to rewrite the flow graph.
            var graphBuilder = graph.ToBuilder();

            // Eliminate switch cases whenever possible.
            SimplifySwitches(graphBuilder, cells);

            // Replace instructions with constants.
            foreach (var selection in graphBuilder.Instructions)
            {
                LatticeCell cell;
                if (cells.TryGetValue(selection, out cell)
                    && cell.IsConstant)
                {
                    selection.Instruction = Instruction.CreateConstant(
                        cell.Value,
                        selection.Instruction.ResultType);
                }
            }

            // Replace block parameters with constants if possible.
            var phiReplacements = new Dictionary<ValueTag, ValueTag>();
            var entryPoint = graphBuilder.GetBasicBlock(graphBuilder.EntryPointTag);

            foreach (var block in graphBuilder.BasicBlocks)
            {
                foreach (var param in block.Parameters)
                {
                    LatticeCell cell;
                    if (cells.TryGetValue(param.Tag, out cell)
                        && cell.IsConstant)
                    {
                        phiReplacements[param.Tag] = entryPoint.InsertInstruction(
                            0,
                            Instruction.CreateConstant(cell.Value, param.Type));
                    }
                }

                var flowInstructions = block.Flow.Instructions;
                var newFlowInstructions = new Instruction[flowInstructions.Count];
                bool anyChanged = false;
                for (int i = 0; i < newFlowInstructions.Length; i++)
                {
                    var cell = EvaluateInstruction(flowInstructions[i], cells, graph);
                    if (cell.IsConstant)
                    {
                        anyChanged = true;
                        newFlowInstructions[i] = Instruction.CreateConstant(
                            cell.Value,
                            flowInstructions[i].ResultType);
                    }
                    else
                    {
                        newFlowInstructions[i] = flowInstructions[i];
                    }
                }

                if (anyChanged)
                {
                    block.Flow = block.Flow.WithInstructions(newFlowInstructions);
                }
            }

            graphBuilder.ReplaceUses(phiReplacements);
            graphBuilder.RemoveDefinitions(phiReplacements.Keys);

            // Remove all instructions from dead blocks and mark the blocks
            // themselves as unreachable.
            foreach (var tag in graphBuilder.BasicBlockTags.Except(liveBlocks).ToArray())
            {
                var block = graphBuilder.GetBasicBlock(tag);

                // Turn the block's flow into unreachable flow.
                block.Flow = UnreachableFlow.Instance;

                // Delete the block's instructions.
                graphBuilder.RemoveInstructionDefinitions(block.InstructionTags);
            }

            return graphBuilder.ToImmutable();
        }

        /// <summary>
        /// Simplify switch flows in the 
        /// </summary>
        /// <param name="graphBuilder">
        /// A mutable control flow graph.
        /// </param>
        /// <param name="cells">
        /// A mapping of values to the lattice cells they evaluate to.
        /// </param>
        private void SimplifySwitches(
            FlowGraphBuilder graphBuilder,
            IReadOnlyDictionary<ValueTag, LatticeCell> cells)
        {
            foreach (var block in graphBuilder.BasicBlocks)
            {
                var switchFlow = block.Flow as SwitchFlow;
                if (switchFlow != null)
                {
                    // We found a switch. Now we just need to figure
                    // out which branches are viable. To do so, we'll
                    // evaluate the switch's condition.
                    var condition = EvaluateInstruction(
                        switchFlow.SwitchValue,
                        cells,
                        graphBuilder.ImmutableGraph);

                    if (condition.IsConstant)
                    {
                        // If a switch flow has a constant condition,
                        // then we pick a single branch and replace the
                        // switch with a jump.
                        var valuesToBranches = switchFlow.ValueToBranchMap;
                        var branch = valuesToBranches.ContainsKey(condition.Value)
                            ? valuesToBranches[condition.Value]
                            : switchFlow.DefaultBranch;

                        block.AppendInstruction(switchFlow.SwitchValue);
                        block.Flow = new JumpFlow(branch);
                    }
                    else if (condition.Kind == LatticeCellKind.NonNull)
                    {
                        // If a switch flow has a non-null condition, then
                        // we can at least rule out all of the null branches.
                        var nullSingleton = new Constant[] { NullConstant.Instance };
                        var newCases = switchFlow.Cases
                            .Where(switchCase => !switchCase.Values.IsSubsetOf(nullSingleton))
                            .ToArray();

                        if (newCases.Length == 0)
                        {
                            block.AppendInstruction(switchFlow.SwitchValue);
                            block.Flow = new JumpFlow(switchFlow.DefaultBranch);
                        }
                        else if (newCases.Length != switchFlow.Cases.Count)
                        {
                            block.Flow = new SwitchFlow(
                                switchFlow.SwitchValue,
                                newCases,
                                switchFlow.DefaultBranch);
                        }
                    }
                }
            }
        }

        private Dictionary<ValueTag, LatticeCell> FillCells(
            FlowGraph graph,
            out IEnumerable<BasicBlockTag> liveBlocks)
        {
            var uses = graph.GetAnalysisResult<ValueUses>();

            // Create a mapping of values to their corresponding lattice cells.
            var valueCells = new Dictionary<ValueTag, LatticeCell>();
            var parameterArgs = new Dictionary<ValueTag, HashSet<ValueTag>>();
            var entryPointBlock = graph.GetBasicBlock(graph.EntryPointTag);

            foreach (var methodParameter in entryPointBlock.ParameterTags)
            {
                // The values of method parameters cannot be inferred by
                // just looking at a method's body. Mark them as bottom cells
                // so we don't fool ourselves into thinking they are constants.
                valueCells[methodParameter] = LatticeCell.Bottom;
            }

            var visitedBlocks = new HashSet<BasicBlockTag>();
            var liveBlockSet = new HashSet<BasicBlockTag>();
            var valueWorklist = new Queue<ValueTag>(entryPointBlock.InstructionTags);
            var flowWorklist = new Queue<BasicBlockTag>();
            flowWorklist.Enqueue(entryPointBlock);
            visitedBlocks.Add(entryPointBlock);
            liveBlockSet.Add(entryPointBlock);

            while (valueWorklist.Count > 0 || flowWorklist.Count > 0)
            {
                // Process all values in the worklist.
                while (valueWorklist.Count > 0)
                {
                    var value = valueWorklist.Dequeue();
                    var cell = GetCellForValue(value, valueCells);

                    var newCell = graph.ContainsInstruction(value)
                        ? UpdateInstructionCell(value, valueCells, graph)
                        : UpdateBlockParameterCell(value, valueCells, parameterArgs);

                    valueCells[value] = newCell;
                    if (cell.Kind != newCell.Kind)
                    {
                        // Visit all instructions and flows that depend on
                        // this instruction.
                        foreach (var item in uses.GetInstructionUses(value))
                        {
                            valueWorklist.Enqueue(item);
                        }
                        foreach (var item in uses.GetFlowUses(value))
                        {
                            flowWorklist.Enqueue(item);
                        }
                    }
                }

                if (flowWorklist.Count > 0)
                {
                    var block = graph.GetBasicBlock(flowWorklist.Dequeue());
                    if (visitedBlocks.Add(block))
                    {
                        // When a block is visited for the first time, add
                        // all of its instructions to the value worklist.
                        foreach (var item in block.InstructionTags)
                        {
                            valueWorklist.Enqueue(item);
                        }
                    }

                    var flow = block.Flow;
                    IReadOnlyList<Branch> branches;
                    if (flow is SwitchFlow)
                    {
                        var switchFlow = (SwitchFlow)flow;
                        var condition = EvaluateInstruction(switchFlow.SwitchValue, valueCells, graph);
                        if (condition.Kind == LatticeCellKind.Top)
                        {
                            // Do nothing for now.
                            branches = EmptyArray<Branch>.Value;
                        }
                        else if (condition.Kind == LatticeCellKind.Constant)
                        {
                            // If a switch flow has a constant condition (for now),
                            // then pick a single branch.
                            var valuesToBranches = switchFlow.ValueToBranchMap;
                            branches = new[]
                            {
                                valuesToBranches.ContainsKey(condition.Value)
                                    ? valuesToBranches[condition.Value]
                                    : switchFlow.DefaultBranch
                            };
                        }
                        else if (condition.Kind == LatticeCellKind.NonNull)
                        {
                            // If a switch flow has a non-null condition, then we
                            // work on all branches except for the null branch, if
                            // it exists.
                            branches = switchFlow.ValueToBranchMap
                                .Where(pair => pair.Key != NullConstant.Instance)
                                .Select(pair => pair.Value)
                                .Concat(new[] { switchFlow.DefaultBranch })
                                .ToArray();
                        }
                        else
                        {
                            // If a switch flow has a bottom condition, then everything
                            // is possible.
                            branches = flow.Branches;
                        }
                    }
                    else
                    {
                        branches = flow.Branches;
                    }

                    // Process the selected branches.
                    foreach (var branch in branches)
                    {
                        // The target of every branch we visit is live.
                        liveBlockSet.Add(branch.Target);

                        // Add the branch target to the worklist of blocks
                        // to process if we haven't processed it already.
                        if (!visitedBlocks.Contains(branch.Target))
                        {
                            flowWorklist.Enqueue(branch.Target);
                        }

                        foreach (var pair in branch.ZipArgumentsWithParameters(graph))
                        {
                            if (pair.Value.IsValue)
                            {
                                HashSet<ValueTag> args;
                                if (!parameterArgs.TryGetValue(pair.Key, out args))
                                {
                                    args = new HashSet<ValueTag>();
                                    parameterArgs[pair.Key] = args;
                                }

                                args.Add(pair.Value.ValueOrNull);
                                valueWorklist.Enqueue(pair.Key);
                            }
                            else
                            {
                                valueCells[pair.Key] = LatticeCell.Bottom;
                            }
                            valueWorklist.Enqueue(pair.Key);
                        }
                    }
                }
            }

            liveBlocks = liveBlockSet;
            return valueCells;
        }

        /// <summary>
        /// Retrieves the lattice cell to which a value evaluates.
        /// </summary>
        /// <returns>The lattice cell for the value.</returns>
        /// <param name="value">The value to inspect.</param>
        /// <param name="cells">A mapping of values to lattice cells.</param>
        private static LatticeCell GetCellForValue(
            ValueTag value,
            IReadOnlyDictionary<ValueTag, LatticeCell> cells)
        {
            LatticeCell result;
            if (cells.TryGetValue(value, out result))
            {
                return result;
            }
            else
            {
                return LatticeCell.Top;
            }
        }

        private LatticeCell EvaluateInstruction(
            Instruction instruction,
            IReadOnlyDictionary<ValueTag, LatticeCell> cells,
            FlowGraph graph)
        {
            if (instruction.Prototype is CopyPrototype)
            {
                // Special case on copy instructions because they're
                // easy to deal with: just return the lattice cell for
                // the argument.
                return GetCellForValue(instruction.Arguments[0], cells);
            }

            var foundTop = false;
            var args = new Constant[instruction.Arguments.Count];
            for (int i = 0; i < args.Length; i++)
            {
                var argCell = GetCellForValue(instruction.Arguments[i], cells);

                if (argCell.Kind == LatticeCellKind.Top)
                {
                    // We can't evaluate this value *yet*. Keep looking
                    // for bottom argument cells and set a flag to record
                    // that this value cannot be evaluated yet.
                    foundTop = true;
                }
                else if (argCell.Kind == LatticeCellKind.Constant)
                {
                    // Yay. We found a compile-time constant.
                    args[i] = argCell.Value;
                }
                else
                {
                    // We can't evaluate this value at compile-time
                    // because one if its arguments is unknown.
                    // Time to early-out.
                    return EvaluateNonConstantInstruction(instruction, graph);
                }
            }

            if (foundTop)
            {
                // We can't evaluate this value yet.
                return LatticeCell.Top;
            }
            else
            {
                // Evaluate the instruction.
                var constant = Evaluate(instruction.Prototype, args);
                if (constant == null)
                {
                    // Turns out we can't evaluate the instruction. But maybe
                    // we can say something sensible about its nullability?
                    return EvaluateNonConstantInstruction(instruction, graph);
                }
                else
                {
                    return LatticeCell.Constant(constant);
                }
            }
        }

        /// <summary>
        /// Evaluates an instruction that has a non-constant value to
        /// a lattice cell.
        /// </summary>
        /// <param name="instruction">The instruction to evaluate.</param>
        /// <param name="graph">The graph that defines the instruction.</param>
        /// <returns>A lattice cell.</returns>
        private static LatticeCell EvaluateNonConstantInstruction(
            Instruction instruction,
            FlowGraph graph)
        {
            // We can't "evaluate" the instruction in a traditional sense,
            // but maybe we can say something sensible about its nullability.

            var nullability = graph.GetAnalysisResult<ValueNullability>();
            if (nullability.IsNonNull(instruction))
            {
                return LatticeCell.NonNull;
            }
            else
            {
                return LatticeCell.Bottom;
            }
        }

        private LatticeCell UpdateInstructionCell(
            ValueTag value,
            Dictionary<ValueTag, LatticeCell> cells,
            FlowGraph graph)
        {
            LatticeCell cell;
            if (!cells.TryGetValue(value, out cell))
            {
                cell = LatticeCell.Top;
            }

            if (cell.Kind == LatticeCellKind.Bottom)
            {
                // Early-out if we're already dealing
                // with a bottom cell.
                return cell;
            }

            var instruction = graph.GetInstruction(value).Instruction;
            return cell.Meet(EvaluateInstruction(instruction, cells, graph));
        }

        private LatticeCell UpdateBlockParameterCell(
            ValueTag value,
            Dictionary<ValueTag, LatticeCell> cells,
            Dictionary<ValueTag, HashSet<ValueTag>> parameterArguments)
        {
            HashSet<ValueTag> args;
            if (!parameterArguments.TryGetValue(value, out args))
            {
                args = new HashSet<ValueTag>();
                parameterArguments[value] = args;
            }

            var cell = cells.ContainsKey(value) ? cells[value] : LatticeCell.Top;
            foreach (var arg in args)
            {
                LatticeCell argCell;
                if (cells.TryGetValue(arg, out argCell))
                {
                    cell = cell.Meet(argCell);
                    if (cell.Kind == LatticeCellKind.Bottom)
                    {
                        // Cell won't change anymore. Early out here.
                        return cell;
                    }
                }
            }
            return cell;
        }

        /// <summary>
        /// An enumeration of status lattice cells can have.
        /// </summary>
        private enum LatticeCellKind
        {
            /// <summary>
            /// Indicates that the cell has not been marked live yet.
            /// </summary>
            Top,

            /// <summary>
            /// Indicates that a constant has been assigned to the cell.
            /// </summary>
            Constant,

            /// <summary>
            /// Indicates that the cell is live and value that is
            /// definitely not <c>null</c>.
            /// </summary>
            NonNull,

            /// <summary>
            /// Indicates that the cell is live but does not have a
            /// constant value. The cell may or may not have a <c>null</c>
            /// value.
            /// </summary>
            Bottom
        }

        /// <summary>
        /// A cell in the sparse conditional constant propagation lattice.
        /// </summary>
        private struct LatticeCell
        {
            private LatticeCell(LatticeCellKind kind, Constant value)
            {
                this.Kind = kind;
                this.Value = value;
            }

            /// <summary>
            /// Gets this cell's status.
            /// </summary>
            public LatticeCellKind Kind { get; private set; }

            /// <summary>
            /// Gets this cell's constant value, if any.
            /// </summary>
            public Constant Value { get; private set; }

            public bool IsConstant => Kind == LatticeCellKind.Constant;

            public LatticeCell Meet(LatticeCell other)
            {
                if (other.Kind == LatticeCellKind.Top)
                {
                    return this;
                }

                switch (Kind)
                {
                    case LatticeCellKind.Top:
                        return other;

                    case LatticeCellKind.Constant:
                        if (other.Kind == LatticeCellKind.Constant
                            && Value.Equals(other.Value))
                        {
                            return this;
                        }
                        else
                        {
                            return Bottom;
                        }

                    case LatticeCellKind.NonNull:
                        if (other.Kind == LatticeCellKind.NonNull)
                        {
                            return this;
                        }
                        else
                        {
                            return Bottom;
                        }

                    case LatticeCellKind.Bottom:
                    default:
                        return this;
                }
            }

            public override string ToString()
            {
                if (Kind == LatticeCellKind.Constant)
                {
                    return $"constant({Value})";
                }
                else
                {
                    return Kind.ToString().ToLowerInvariant();
                }
            }

            public static LatticeCell Top =>
                new LatticeCell(LatticeCellKind.Top, null);

            public static LatticeCell Bottom =>
                new LatticeCell(LatticeCellKind.Bottom, null);

            public static LatticeCell NonNull =>
                new LatticeCell(LatticeCellKind.NonNull, null);

            public static LatticeCell Constant(Constant value) =>
                new LatticeCell(LatticeCellKind.Constant, value);
        }
    }
}
