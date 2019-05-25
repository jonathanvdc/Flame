using System.Collections.Generic;
using Flame.Compiler.Flow;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// The result of a lattice analysis.
    /// </summary>
    public struct LatticeAnalysisResult<TCell>
    {
        /// <summary>
        /// Creates a lattice analysis result.
        /// </summary>
        /// <param name="valueCells">A mapping of values to lattice cells.</param>
        /// <param name="liveBlocks">The set of all live basic blocks.</param>
        public LatticeAnalysisResult(
            IReadOnlyDictionary<ValueTag, TCell> valueCells,
            IEnumerable<BasicBlockTag> liveBlocks)
        {
            this.ValueCells = valueCells;
            this.LiveBlocks = liveBlocks;
        }

        /// <summary>
        /// Gets a mapping of values to lattice cells, as computed
        /// by the analysis.
        /// </summary>
        /// <value>A mapping of values to lattice cells.</value>
        public IReadOnlyDictionary<ValueTag, TCell> ValueCells { get; private set; }

        /// <summary>
        /// Gets the set of all basic blocks that are live according
        /// to the analysis.
        /// </summary>
        /// <value>A set of basic blocks.</value>
        public IEnumerable<BasicBlockTag> LiveBlocks { get; private set; }
    }

    /// <summary>
    /// A base class for flow-sensitive data analyses that assign
    /// analysis results to values by repeatedly computing lattice
    /// meet operations.
    /// </summary>
    /// <typeparam name="TCell">
    /// The type of a lattice cell. These are the cells of the
    /// bounded lattice on which a meet operation is defined.
    /// The analysis produces a mapping of values to lattice cells.
    /// </typeparam>
    public abstract class LatticeAnalysis<TCell> : IFlowGraphAnalysis<LatticeAnalysisResult<TCell>>
    {
        /// <summary>
        /// Gets the top cell of the bounded lattice. The top cell
        /// is the most permissive lattice cell that can be assigned
        /// to a value: computing the meet of the top cell and any
        /// other cell produces the latter.
        /// </summary>
        /// <value>The top cell of the bounded lattice.</value>
        public abstract TCell Top { get; }

        /// <summary>
        /// Gets the bottom cell of the bounded lattice. The top cell
        /// is the least permissive lattice cell that can be assigned
        /// to a value: computing the meet of the bottom cell and any
        /// other cell produces the former.
        /// </summary>
        /// <value>The bottom cell of the bounded lattice.</value>
        public abstract TCell Bottom { get; }

        /// <summary>
        /// Computes the meet of two lattice cells.
        /// </summary>
        /// <param name="first">A lattice cell.</param>
        /// <param name="second">Another lattice cell.</param>
        /// <returns>A new lattice cell.</returns>
        /// <remarks>
        /// The meet operator must adhere to a number of axioms; otherwise,
        /// the analysis might report incorrect results or end up in an infinite
        /// loop. See Wikipedia for the minutiae.
        /// https://en.wikipedia.org/wiki/Lattice_(order)#Lattices_as_algebraic_structures
        ///
        /// These axioms are always satisfied if the meet operator corresponds
        /// to the max on a total order, i.e., there are finitely many lattice
        /// cells E_1, E_2, ..., E_n and E_1 &lt; E_2 &lt; ... &lt; E_n.
        /// </remarks>
        public abstract TCell Meet(TCell first, TCell second);

        /// <summary>
        /// Evaluates an instruction to a lattice cell, given the values
        /// other cells evaluate to.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to evaluate.
        /// </param>
        /// <param name="cells">
        /// The lattice cells currently assigned to values in the graph.
        /// </param>
        /// <returns>
        /// A lattice cell.
        /// </returns>
        public abstract TCell Evaluate(
            NamedInstruction instruction,
            IReadOnlyDictionary<ValueTag, TCell> cells);

        /// <summary>
        /// Given block flow, computes its live branches given the cells to
        /// which values evaluate.
        /// </summary>
        /// <param name="flow">
        /// The block flow to consider.
        /// </param>
        /// <param name="cells">
        /// The lattice cells currently assigned to values in the graph.
        /// </param>
        /// <param name="graph">
        /// The control-flow graph that defines <paramref name="flow"/>.
        /// </param>
        /// <returns>A sequence of live branches.</returns>
        public virtual IEnumerable<Branch> GetLiveBranches(
            BlockFlow flow,
            IReadOnlyDictionary<ValueTag, TCell> cells,
            FlowGraph graph)
        {
            return flow.Branches;
        }

        /// <inheritdoc/>
        public LatticeAnalysisResult<TCell> Analyze(FlowGraph graph)
        {
            return FillCells(graph);
        }

        /// <inheritdoc/>
        public LatticeAnalysisResult<TCell> AnalyzeWithUpdates(
            FlowGraph graph,
            LatticeAnalysisResult<TCell> previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }

        private LatticeAnalysisResult<TCell> FillCells(FlowGraph graph)
        {
            var uses = graph.GetAnalysisResult<ValueUses>();

            // Create a mapping of values to their corresponding lattice cells.
            var valueCells = new Dictionary<ValueTag, TCell>();
            var parameterArgs = new Dictionary<ValueTag, HashSet<ValueTag>>();
            var entryPointBlock = graph.GetBasicBlock(graph.EntryPointTag);

            // Assign 'top' to all values in the graph.
            foreach (var tag in graph.ValueTags)
            {
                valueCells[tag] = Top;
            }

            foreach (var methodParameter in entryPointBlock.ParameterTags)
            {
                // The values of method parameters cannot be inferred by
                // just looking at a method's body. Mark them as bottom cells
                // so we don't fool ourselves into thinking they are constants.
                valueCells[methodParameter] = Bottom;
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
                    var cell = valueCells[value];

                    var newCell = graph.ContainsInstruction(value)
                        ? UpdateInstructionCell(value, valueCells, graph)
                        : UpdateBlockParameterCell(value, valueCells, parameterArgs);

                    valueCells[value] = newCell;
                    if (!Equals(cell, newCell))
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

                    // Process the live branches.
                    foreach (var branch in GetLiveBranches(block.Flow, valueCells, graph))
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
                                valueCells[pair.Key] = Bottom;
                            }
                            valueWorklist.Enqueue(pair.Key);
                        }
                    }
                }
            }

            return new LatticeAnalysisResult<TCell>(valueCells, liveBlockSet);
        }

        private TCell UpdateInstructionCell(
            ValueTag value,
            Dictionary<ValueTag, TCell> cells,
            FlowGraph graph)
        {
            TCell cell;
            if (!cells.TryGetValue(value, out cell))
            {
                cell = Top;
            }

            if (Equals(cell, Bottom))
            {
                // Early-out if we're already dealing
                // with a bottom cell.
                return cell;
            }

            var instruction = graph.GetInstruction(value);
            return Meet(cell, Evaluate(instruction, cells));
        }

        private TCell UpdateBlockParameterCell(
            ValueTag value,
            Dictionary<ValueTag, TCell> cells,
            Dictionary<ValueTag, HashSet<ValueTag>> parameterArguments)
        {
            HashSet<ValueTag> args;
            if (!parameterArguments.TryGetValue(value, out args))
            {
                args = new HashSet<ValueTag>();
                parameterArguments[value] = args;
            }

            var cell = cells.ContainsKey(value) ? cells[value] : Top;
            foreach (var arg in args)
            {
                TCell argCell;
                if (cells.TryGetValue(arg, out argCell))
                {
                    cell = Meet(cell, argCell);
                    if (Equals(cell, Bottom))
                    {
                        // Cell won't change anymore. Early out here.
                        return cell;
                    }
                }
            }
            return cell;
        }
    }
}
