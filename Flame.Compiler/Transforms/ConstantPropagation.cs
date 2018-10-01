using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;

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

        public override FlowGraph Apply(FlowGraph graph)
        {
            // Do the fancy analysis.
            var cells = FillCells(graph);

            // Rewrite the flow graph.
            var graphBuilder = graph.ToBuilder();

            foreach (var selection in graphBuilder.Instructions)
            {
                // Replace instructions with constants.
                LatticeCell cell;
                if (cells.TryGetValue(selection, out cell)
                    && cell.IsConstant)
                {
                    selection.Instruction = Instruction.CreateConstant(
                        cell.Value,
                        selection.Instruction.ResultType);
                }
            }

            var phiReplacements = new Dictionary<ValueTag, ValueTag>();
            var entryPoint = graphBuilder.GetBasicBlock(graphBuilder.EntryPointTag);

            foreach (var block in graphBuilder.BasicBlocks)
            {
                // Replace block parameters with constants if possible.
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
            return graphBuilder.ToImmutable();
        }

        private Dictionary<ValueTag, LatticeCell> FillCells(FlowGraph graph)
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
            var valueWorklist = new Queue<ValueTag>(entryPointBlock.InstructionTags);
            var flowWorklist = new Queue<BasicBlockTag>();
            flowWorklist.Enqueue(entryPointBlock);
            visitedBlocks.Add(entryPointBlock);

            while (valueWorklist.Count > 0 || flowWorklist.Count > 0)
            {
                while (valueWorklist.Count > 0)
                {
                    var value = valueWorklist.Dequeue();
                    LatticeCell cell;
                    if (!valueCells.TryGetValue(value, out cell))
                    {
                        cell = LatticeCell.Top;
                    }

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
                        if (!visitedBlocks.Contains(block))
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

                                args.Add(pair.Key);
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

            return valueCells;
        }

        private LatticeCell EvaluateInstruction(
            Instruction instruction,
            Dictionary<ValueTag, LatticeCell> cells,
            FlowGraph graph)
        {
            var foundTop = false;
            var args = new Constant[instruction.Arguments.Count];
            for (int i = 0; i < args.Length; i++)
            {
                LatticeCell argCell;
                if (!cells.TryGetValue(instruction.Arguments[i], out argCell))
                {
                    argCell = LatticeCell.Top;
                }

                if (argCell.Kind == LatticeCellKind.Top)
                {
                    // We can't evaluate this value *yet*. Keep looking
                    // for bottom argument cells and set a flag to record
                    // that this value cannot be evaluated yet.
                    foundTop = true;
                }
                else if (argCell.Kind == LatticeCellKind.Bottom)
                {
                    // We can't evaluate this value at compile-time
                    // because one if its arguments is unknown.
                    // Time to early-out.
                    return LatticeCell.Bottom;
                }
                else
                {
                    args[i] = argCell.Value;
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
                    // Turns out we can't evaluate the instruction.
                    return LatticeCell.Bottom;
                }
                else
                {
                    return LatticeCell.Constant(constant);
                }
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

            var cell = LatticeCell.Top;
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
            /// Indicates that the cell is live but does not have a
            /// constant value.
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
                switch (Kind)
                {
                    case LatticeCellKind.Top:
                        return other;
                    case LatticeCellKind.Constant:
                        if (Value.Equals(other.Value))
                            return this;
                        else
                            return Bottom;
                    case LatticeCellKind.Bottom:
                    default:
                        return this;
                }
            }

            public static LatticeCell Top =>
                new LatticeCell(LatticeCellKind.Top, null);

            public static LatticeCell Bottom =>
                new LatticeCell(LatticeCellKind.Bottom, null);

            public static LatticeCell Constant(Constant value) =>
                new LatticeCell(LatticeCellKind.Constant, value);
        }
    }
}
