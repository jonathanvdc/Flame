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
        public ConstantPropagation()
            : this(EvaluateDefault)
        { }

        /// <summary>
        /// Creates a constant propagation transform that uses a particular
        /// evaluation function.
        /// </summary>
        /// <param name="evaluate">
        /// The evaluation function to use. It evaluates an instruction that
        /// takes a list of constant arguments. It returns <c>null</c> if the
        /// instruction cannot be evaluated; otherwise, it returns the constant
        /// to which it was evaluated.
        /// </param>
        public ConstantPropagation(
            Func<InstructionPrototype, IReadOnlyList<Constant>, Constant> evaluate)
        {
            this.Analyzer = new Analysis(evaluate);
        }

        /// <summary>
        /// Gets the lattice-based analysis used to perform constant propagation.
        /// </summary>
        /// <value>The constant propagation analysis.</value>
        private Analysis Analyzer { get; set; }

        /// <summary>
        /// Evaluates an instruction that takes a list of constant arguments.
        /// Returns <c>null</c> if the instruction cannot be evaluated.
        /// </summary>
        public Func<InstructionPrototype, IReadOnlyList<Constant>, Constant> Evaluate
            => Analyzer.EvaluateAsConstant;

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
        /// <returns>
        /// <c>null</c> if the instruction cannot be evaluated; otherwise, the constant
        /// to which the instruction evaluates.
        /// </returns>
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
                var intrinsicProto = (IntrinsicPrototype)prototype;

                Constant result;
                if (ArithmeticIntrinsics.IsArithmeticIntrinsicPrototype(intrinsicProto)
                    && ArithmeticIntrinsics.TryEvaluate(intrinsicProto, arguments, out result))
                {
                    return result;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // Do the fancy analysis.
            var analysis = Analyzer.Analyze(graph);
            var cells = analysis.ValueCells;
            var liveBlocks = analysis.LiveBlocks;

            // Get ready to rewrite the flow graph.
            var graphBuilder = graph.ToBuilder();

            // Eliminate switch cases whenever possible.
            SimplifySwitches(graphBuilder, cells);

            // Replace instructions with constants.
            foreach (var selection in graphBuilder.NamedInstructions)
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
                    var cell = Analyzer.Evaluate(flowInstructions[i], cells, graph);
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
                    var condition = Analyzer.Evaluate(
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

        /// <summary>
        /// The lattice-based analysis that underpins the constant propagation optimization.
        /// </summary>
        private sealed class Analysis : LatticeAnalysis<LatticeCell>
        {
            public Analysis(
                Func<InstructionPrototype, IReadOnlyList<Constant>, Constant> evaluateAsConstant)
            {
                this.EvaluateAsConstant = evaluateAsConstant;
            }

            /// <summary>
            /// Evaluates an instruction that takes a list of constant arguments.
            /// Returns <c>null</c> if the instruction cannot be evaluated.
            /// </summary>
            public Func<InstructionPrototype, IReadOnlyList<Constant>, Constant> EvaluateAsConstant { get; private set; }

            public override LatticeCell Top => LatticeCell.Top;

            public override LatticeCell Bottom => LatticeCell.Bottom;

            public override LatticeCell Evaluate(
                NamedInstruction instruction,
                IReadOnlyDictionary<ValueTag, LatticeCell> cells)
            {
                return Evaluate(instruction.Instruction, cells, instruction.Block.Graph);
            }

            public LatticeCell Evaluate(
                Instruction instruction,
                IReadOnlyDictionary<ValueTag, LatticeCell> cells,
                FlowGraph graph)
            {
                if (instruction.Prototype is CopyPrototype)
                {
                    // Special case on copy instructions because they're
                    // easy to deal with: just return the lattice cell for
                    // the argument.
                    return cells[instruction.Arguments[0]];
                }

                var foundTop = false;
                var args = new Constant[instruction.Arguments.Count];
                for (int i = 0; i < args.Length; i++)
                {
                    var argCell = cells[instruction.Arguments[i]];

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
                    var constant = EvaluateAsConstant(instruction.Prototype, args);
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

            public override LatticeCell Meet(LatticeCell first, LatticeCell second)
            {
                return first.Meet(second);
            }

            public override IEnumerable<Branch> GetLiveBranches(
                BlockFlow flow,
                IReadOnlyDictionary<ValueTag, LatticeCell> cells,
                FlowGraph graph)
            {
                if (flow is SwitchFlow)
                {
                    var switchFlow = (SwitchFlow)flow;
                    var condition = Evaluate(switchFlow.SwitchValue, cells, graph);
                    if (condition.Kind == LatticeCellKind.Top)
                    {
                        // Do nothing for now.
                        return EmptyArray<Branch>.Value;
                    }
                    else if (condition.Kind == LatticeCellKind.Constant)
                    {
                        // If a switch flow has a constant condition (for now),
                        // then pick a single branch.
                        var valuesToBranches = switchFlow.ValueToBranchMap;
                        return new[]
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
                        return switchFlow.ValueToBranchMap
                            .Where(pair => pair.Key != NullConstant.Instance)
                            .Select(pair => pair.Value)
                            .Concat(new[] { switchFlow.DefaultBranch })
                            .ToArray();
                    }
                    else
                    {
                        // If a switch flow has a bottom condition, then everything
                        // is possible.
                        return flow.Branches;
                    }
                }
                else
                {
                    return flow.Branches;
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
        private struct LatticeCell : IEquatable<LatticeCell>
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

            public bool Equals(LatticeCell other)
            {
                return Kind == other.Kind
                    && Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return obj is LatticeCell && Equals((LatticeCell)obj);
            }

            public override int GetHashCode()
            {
                if (Value == null)
                {
                    return Kind.GetHashCode();
                }
                else
                {
                    return Kind.GetHashCode() ^ Value.GetHashCode();
                }
            }
        }
    }
}
