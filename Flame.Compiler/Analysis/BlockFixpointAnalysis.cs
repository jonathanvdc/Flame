using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flame.Compiler.Analysis
{
    /// <summary>
    /// A base class for analyses that process blocks until they reach a fixpoint.
    /// </summary>
    /// <typeparam name="TBlockState">The result of analyzing a single block.</typeparam>
    public abstract class BlockFixpointAnalysis<TBlockState>
        : IFlowGraphAnalysis<BlockFixpointAnalysis<TBlockState>.Result>
    {
        /// <summary>
        /// The result of a block fixpoint analysis.
        /// </summary>
        public struct Result
        {
            internal Result(IReadOnlyDictionary<BasicBlockTag, TBlockState> results)
            {
                this.BlockResults = results;
            }

            /// <summary>
            /// Gets a mapping of basic blocks to block analysis results.
            /// </summary>
            /// <value>A mapping of basic blocks to analysis results.</value>
            public IReadOnlyDictionary<BasicBlockTag, TBlockState> BlockResults { get; private set; }
        }

        /// <summary>
        /// Tests if two block states are the same.
        /// </summary>
        /// <param name="first">
        /// The first block state to compare.
        /// </param>
        /// <param name="second">
        /// The second block state to compare.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="first"/> equals <paramref name="second"/>; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool Equals(TBlockState first, TBlockState second);

        /// <summary>
        /// Merges two block states. This method unifies outgoing block
        /// states into a single input block state.
        /// </summary>
        /// <param name="first">
        /// A first block state.
        /// </param>
        /// <param name="second">
        /// A second block state.
        /// </param>
        /// <returns>A merged block state.</returns>
        public abstract TBlockState Merge(TBlockState first, TBlockState second);

        /// <summary>
        /// Creates an input block state for an entry point block.
        /// </summary>
        /// <param name="entryPoint">The entry point block to create a state for.</param>
        /// <returns>An input block state for the entry point.</returns>
        public abstract TBlockState CreateEntryPointInput(BasicBlock entryPoint);

        /// <summary>
        /// Processes a block's contents.
        /// </summary>
        /// <param name="block">A block to process.</param>
        /// <param name="input">The block's input state.</param>
        /// <returns>The block's output state.</returns>
        public abstract TBlockState Process(BasicBlock block, TBlockState input);

        /// <summary>
        /// Gets a block's outgoing inputs: a sequence of key-value pairs where the
        /// keys list affected blocks and the values list values that should be
        /// merged with those blocks' inputs.
        /// </summary>
        /// <param name="block">A basic block.</param>
        /// <param name="output">
        /// The output for that basic block, as computed by the <c>Process</c> method.
        /// </param>
        /// <returns>A sequence of key-value pairs.</returns>
        public virtual IEnumerable<KeyValuePair<BasicBlockTag, TBlockState>> GetOutgoingInputs(
            BasicBlock block,
            TBlockState output)
        {
            var results = new Dictionary<BasicBlockTag, TBlockState>();
            foreach (var target in block.Flow.BranchTargets)
            {
                results[target] = output;
            }
            return results;
        }

        /// <inheritdoc/>
        public Result Analyze(FlowGraph graph)
        {
            // Create the input state for the entry point.
            var inputs = new Dictionary<BasicBlockTag, TBlockState>();
            inputs[graph.EntryPointTag] = CreateEntryPointInput(graph.EntryPoint);

            var outputs = ImmutableDictionary<BasicBlockTag, TBlockState>.Empty.ToBuilder();

            var worklist = new HashSet<BasicBlockTag>() { graph.EntryPointTag };

            while (worklist.Count > 0)
            {
                // Pop a block from the worklist.
                var blockTag = worklist.First();
                worklist.Remove(blockTag);

                // Process the block.
                var block = graph.GetBasicBlock(blockTag);
                var blockOutput = Process(block, inputs[block]);
                outputs[block] = blockOutput;

                // Process the block's outgoing branches.
                foreach (var pair in GetOutgoingInputs(block, blockOutput))
                {
                    var target = pair.Key;
                    TBlockState targetInput;
                    if (inputs.TryGetValue(target, out targetInput))
                    {
                        // If a branch target has already been processed, then we will
                        // merge this block's output with the branch target's input. If
                        // they are the same, then we'll do nothing. Otherwise, we'll
                        // re-process the branch with the new input.
                        var merged = Merge(pair.Value, targetInput);
                        if (!Equals(merged, targetInput))
                        {
                            inputs[target] = merged;
                            worklist.Add(target);
                        }
                    }
                    else
                    {
                        // If the branch target hasn't been processed yet, then it's about
                        // time that we do. Add it to the worklist.
                        inputs[target] = pair.Value;
                        worklist.Add(target);
                    }
                }
            }

            return new Result(outputs.ToImmutable());
        }

        /// <inheritdoc/>
        public Result AnalyzeWithUpdates(
            FlowGraph graph,
            Result previousResult,
            IReadOnlyList<FlowGraphUpdate> updates)
        {
            return Analyze(graph);
        }
    }
}
