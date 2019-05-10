using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Transforms;

namespace Flame.Compiler
{
    /// <summary>
    /// A mutable view of an immutable control-flow graph.
    /// </summary>
    public sealed class FlowGraphBuilder
    {
        /// <summary>
        /// Creates a control-flow graph builder that contains
        /// only an empty entry point block.
        /// </summary>
        public FlowGraphBuilder()
            : this(new FlowGraph())
        { }

        /// <summary>
        /// Creates a control-flow graph builder from an
        /// immutable control-flow graph.
        /// </summary>
        /// <param name="graph">An immutable control-flow graph.</param>
        public FlowGraphBuilder(FlowGraph graph)
        {
            this.ImmutableGraph = graph;
        }

        /// <summary>
        /// Gets or sets the control-flow graph wrapped by this builder.
        /// </summary>
        /// <returns>The control-flow graph.</returns>
        internal FlowGraph ImmutableGraph { get; set; }

        /// <summary>
        /// Gets the tag of the entry point block.
        /// </summary>
        /// <returns>The tag of the entry point block.</returns>
        public BasicBlockTag EntryPointTag
        {
            get
            {
                return ImmutableGraph.EntryPointTag;
            }
            set
            {
                ImmutableGraph = ImmutableGraph.WithEntryPoint(value);
            }
        }

        /// <summary>
        /// Gets the entry point block.
        /// </summary>
        /// <returns>The entry point block.</returns>
        public BasicBlockBuilder EntryPoint => GetBasicBlock(EntryPointTag);

        /// <summary>
        /// Gets a sequence of all basic block tags in this control-flow graph.
        /// </summary>
        public IEnumerable<BasicBlockTag> BasicBlockTags => ImmutableGraph.BasicBlockTags;

        /// <summary>
        /// Gets a sequence of all basic blocks in this control-flow graph.
        /// </summary>
        /// <returns>All basic blocks.</returns>
        public IEnumerable<BasicBlockBuilder> BasicBlocks =>
            BasicBlockTags.Select(GetBasicBlock);

        /// <summary>
        /// Gets a sequence of all instruction tags in this control-flow graph.
        /// </summary>
        public IEnumerable<ValueTag> InstructionTags => ImmutableGraph.InstructionTags;

        /// <summary>
        /// Gets a sequence of all parameter tags in this control-flow graph.
        /// </summary>
        public IEnumerable<ValueTag> ParameterTags => ImmutableGraph.ParameterTags;

        /// <summary>
        /// Gets a sequence of all value tags in this control-flow graph.
        /// This sequence includes both instruction values and
        /// basic block parameter values.
        /// </summary>
        public IEnumerable<ValueTag> ValueTags => ImmutableGraph.ValueTags;

        /// <summary>
        /// Gets a sequence of all named instructions in this control-flow graph.
        /// Anonymous instructions as defined by block flow are not included.
        /// </summary>
        /// <returns>All named instructions.</returns>
        public IEnumerable<NamedInstructionBuilder> NamedInstructions =>
            InstructionTags.Select(GetInstruction);

        /// <summary>
        /// Gets a sequence of all anonymous instructions defined by block flow
        /// in this control-flow graph.
        /// </summary>
        /// <returns>All anonymous instructions.</returns>
        public IEnumerable<InstructionBuilder> AnonymousInstructions =>
            BasicBlocks.SelectMany(block => block.Flow.GetInstructionBuilders(block));

        /// <summary>
        /// Gets a sequence of all instructions defined in this control-flow graph,
        /// including both named and anonymous instructions.
        /// </summary>
        /// <returns>All instructions in this control-flow graph.</returns>
        public IEnumerable<InstructionBuilder> Instructions =>
            NamedInstructions.Concat(AnonymousInstructions);

        /// <summary>
        /// Registers a flow graph analysis with this graph.
        /// </summary>
        /// <param name="analysis">The analysis to register.</param>
        /// <typeparam name="T">
        /// The type of result produced by the analysis.
        /// </typeparam>
        public void AddAnalysis<T>(IFlowGraphAnalysis<T> analysis)
        {
            ImmutableGraph = ImmutableGraph.WithAnalysis<T>(analysis);
        }

        /// <summary>
        /// Gets an analysis result based on its type.
        /// </summary>
        /// <typeparam name="T">
        /// The type of analysis result to fetch or compute.
        /// </typeparam>
        /// <returns>
        /// An analysis result.
        /// </returns>
        public T GetAnalysisResult<T>()
        {
            return ImmutableGraph.GetAnalysisResult<T>();
        }

        /// <summary>
        /// Tries to get an analysis result of a particular type.
        /// </summary>
        /// <param name="result">
        /// The analysis result, if one can be fetched or computed.
        /// </param>
        /// <typeparam name="T">
        /// The type of analysis result to fetch or compute.
        /// </typeparam>
        /// <returns>
        /// <c>true</c> if there is an analyzer to compute the result;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetAnalysisResult<T>(out T result)
        {
            return ImmutableGraph.TryGetAnalysisResult<T>(out result);
        }

        /// <summary>
        /// Tells if this flow graph has an analysis that produces
        /// a particular type of result.
        /// </summary>
        /// <typeparam name="T">
        /// The type of analysis result that is sought.
        /// </typeparam>
        /// <returns>
        /// <c>true</c> if a registered analysis produces a result of type <c>T</c>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool HasAnalysisFor<T>()
        {
            return ImmutableGraph.HasAnalysisFor<T>();
        }

        /// <summary>
        /// Gets the analysis, registered with this control-flow graph,
        /// that produced a particular type of result.
        /// </summary>
        /// <typeparam name="T">
        /// The type of analysis result that is sought.
        /// </typeparam>
        /// <returns>
        /// An analysis.
        /// </returns>
        public IFlowGraphAnalysis<T> GetAnalysisFor<T>()
        {
            return ImmutableGraph.GetAnalysisFor<T>();
        }

        /// <summary>
        /// Adds an empty basic block to this flow-graph builder.
        /// </summary>
        /// <param name="name">The (preferred) name of the basic block's tag.</param>
        /// <returns>An empty basic block builder.</returns>
        public BasicBlockBuilder AddBasicBlock(string name)
        {
            var newBlock = ImmutableGraph.AddBasicBlock(name);
            ImmutableGraph = newBlock.Graph;
            return GetBasicBlock(newBlock.Tag);
        }

        /// <summary>
        /// Adds an empty basic block to this flow-graph builder.
        /// </summary>
        /// <returns>An empty basic block builder.</returns>
        public BasicBlockBuilder AddBasicBlock()
        {
            return AddBasicBlock("");
        }

        /// <summary>
        /// Removes the basic block with a particular tag from this
        /// control-flow graph.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        public void RemoveBasicBlock(BasicBlockTag tag)
        {
            ImmutableGraph = ImmutableGraph.RemoveBasicBlock(tag);
        }

        /// <summary>
        /// Removes a particular instruction from this control-flow graph.
        /// Returns a new control-flow graph that does not contain the
        /// instruction.
        /// </summary>
        /// <param name="instructionTag">The tag of the instruction to remove.</param>
        /// <returns>
        /// A control-flow graph that no longer contains the instruction.
        /// </returns>
        public void RemoveInstruction(ValueTag instructionTag)
        {
            ImmutableGraph = ImmutableGraph.RemoveInstruction(instructionTag);
        }

        /// <summary>
        /// Replaces all uses of values with other values.
        /// The values to replace are encoded as keys in a
        /// dictionary and the values to replace them with as
        /// values in that same dictionary.
        /// </summary>
        /// <param name="replacementMap">
        /// A mapping of values to replacement values.
        /// </param>
        public void ReplaceUses(
            IReadOnlyDictionary<ValueTag, ValueTag> replacementMap)
        {
            ImmutableGraph = ImmutableGraph.ReplaceUses(replacementMap);
        }

        /// <summary>
        /// Removes the definitions for a set of values
        /// from this flow graph.
        /// </summary>
        /// <param name="valuesToRemove">
        /// A set of values whose definitions are to be eliminated
        /// from the flow graph. These values can refer to instructions
        /// and basic block parameters.
        /// </param>
        public void RemoveDefinitions(IEnumerable<ValueTag> valuesToRemove)
        {
            ImmutableGraph = ImmutableGraph.RemoveDefinitions(valuesToRemove);
        }

        /// <summary>
        /// Removes the definitions for a set of instructions
        /// from this flow graph.
        /// </summary>
        /// <param name="instructionsToRemove">
        /// A set of values whose definitions are to be eliminated
        /// from the flow graph. These values may only refer to
        /// instructions.
        /// </param>
        /// <remark>
        /// This method incurs less overhead than RemoveDefinitions but
        /// only works for instruction definitions, not for basic block
        /// parameter definitions.
        /// </remark>
        public void RemoveInstructionDefinitions(IEnumerable<ValueTag> instructionsToRemove)
        {
            ImmutableGraph = ImmutableGraph.RemoveInstructionDefinitions(instructionsToRemove);
        }

        /// <summary>
        /// Applies an intraprocedural optimization to this flow graph.
        /// </summary>
        /// <param name="optimization">
        /// The transform to apply.
        /// </param>
        public void Transform(IntraproceduralOptimization optimization)
        {
            ImmutableGraph = ImmutableGraph.Transform(optimization);
        }

        /// <summary>
        /// Applies a sequence of intraprocedural optimizations to this flow graph.
        /// </summary>
        /// <param name="optimizations">
        /// The transforms to apply.
        /// </param>
        public void Transform(IEnumerable<IntraproceduralOptimization> optimizations)
        {
            ImmutableGraph = ImmutableGraph.Transform(optimizations);
        }

        /// <summary>
        /// Applies a sequence of intraprocedural optimizations to this flow graph.
        /// </summary>
        /// <param name="optimizations">
        /// The transforms to apply.
        /// </param>
        public void Transform(params IntraproceduralOptimization[] optimizations)
        {
            ImmutableGraph = ImmutableGraph.Transform(optimizations);
        }

        /// <summary>
        /// Gets the basic block with a particular tag.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        /// <returns>A basic block.</returns>
        public BasicBlockBuilder GetBasicBlock(BasicBlockTag tag)
        {
            return new BasicBlockBuilder(this, tag);
        }

        /// <summary>
        /// Gets the instruction with a particular tag.
        /// </summary>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>A named instruction.</returns>
        public NamedInstructionBuilder GetInstruction(ValueTag tag)
        {
            NamedInstructionBuilder result;
            if (TryGetInstruction(tag, out result))
            {
                return result;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Flow graph does not define an instruction with tag '{tag}'.");
            }
        }

        /// <summary>
        /// Gets this control flow graph builder's equivalent of a shared
        /// instruction in another control flow graph.
        /// </summary>
        /// <param name="instruction">
        /// The instruction builder to find an equivalent for.
        /// </param>
        /// <returns>An instruction builder.</returns>
        /// <remark>
        /// This method is designed to be used in conjuction with <c>TryForkAndMerge</c>.
        /// </remark>
        public InstructionBuilder GetInstruction(InstructionBuilder instruction)
        {
            if (instruction is NamedInstructionBuilder)
            {
                return GetInstruction(((NamedInstructionBuilder)instruction).Tag);
            }
            else
            {
                var anonymous = (FlowInstructionBuilder)instruction;
                return GetBasicBlock(anonymous.Block).Flow.GetInstructionBuilder(anonymous.Block, 0);
            }
        }

        /// <summary>
        /// Tries to get an instruction with a particular tag, if it exists in this
        /// control-flow graph.
        /// </summary>
        /// <param name="tag">The instruction's tag.</param>
        /// <param name="result">
        /// The named instruction, if it exists in this control-flow graph.
        /// </param>
        /// <returns><c>true</c> if the instruction exists; otherwise, <c>false</c>.</returns>
        public bool TryGetInstruction(ValueTag tag, out NamedInstructionBuilder result)
        {
            if (ContainsInstruction(tag))
            {
                result = new NamedInstructionBuilder(this, tag);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Checks if this control-flow graph contains a basic block
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains a basic block
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsBasicBlock(BasicBlockTag tag)
        {
            return ImmutableGraph.ContainsBasicBlock(tag);
        }

        /// <summary>
        /// Checks if this control-flow graph contains an instruction
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains an instruction
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsInstruction(ValueTag tag)
        {
            return ImmutableGraph.ContainsInstruction(tag);
        }

        /// <summary>
        /// Checks if this control-flow graph contains a basic block parameter
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">The parameter's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains a basic block parameter
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsBlockParameter(ValueTag tag)
        {
            return ImmutableGraph.ContainsBlockParameter(tag);
        }

        /// <summary>
        /// Checks if this control-flow graph contains an instruction
        /// or basic block parameter with a particular tag.
        /// </summary>
        /// <param name="tag">The value's tag.</param>
        /// <returns>
        /// <c>true</c> if this control-flow graph contains a value
        /// with the given tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsValue(ValueTag tag)
        {
            return ImmutableGraph.ContainsValue(tag);
        }

        /// <summary>
        /// Gets the type of a value in this graph.
        /// </summary>
        /// <param name="tag">The value's tag.</param>
        /// <returns>The value's type.</returns>
        public IType GetValueType(ValueTag tag)
        {
            return ImmutableGraph.GetValueType(tag);
        }

        /// <summary>
        /// Gets basic block that defines a value with a
        /// particular tag.
        /// </summary>
        /// <param name="tag">The tag of the value to look for.</param>
        /// <returns>The basic block that defines the value.</returns>
        public BasicBlockBuilder GetValueParent(ValueTag tag)
        {
            return GetBasicBlock(ImmutableGraph.GetValueParent(tag).Tag);
        }

        /// <summary>
        /// Turns this control-flow graph builder into an immutable
        /// control-flow graph.
        /// </summary>
        /// <returns>An immutable control-flow graph.</returns>
        public FlowGraph ToImmutable()
        {
            return ImmutableGraph;
        }

        /// <summary>
        /// Includes a control-flow graph in this control-flow graph.
        /// Any values and blocks defined by the graph to include are
        /// renamed in order to avoid conflicts with tags in this graph.
        /// </summary>
        /// <param name="graph">
        /// The graph to include in this graph.
        /// </param>
        /// <param name="rewriteReturnFlow">
        /// Rewrites 'return' flow.
        /// </param>
        /// <returns>
        /// The tag of the imported graph's entry point.
        /// </returns>
        public BasicBlockTag Include(
            FlowGraph graph,
            Func<ReturnFlow, BasicBlockBuilder, BlockFlow> rewriteReturnFlow)
        {
            return Include(graph, rewriteReturnFlow, null);
        }

        /// <summary>
        /// Includes a control-flow graph in this control-flow graph.
        /// Any values and blocks defined by the graph to include are
        /// renamed in order to avoid conflicts with tags in this graph.
        /// Instructions that may throw an exception are wrapped in 'try'
        /// flow.
        /// </summary>
        /// <param name="graph">
        /// The graph to include in this graph.
        /// </param>
        /// <param name="rewriteReturnFlow">
        /// Rewrites 'return' flow.
        /// </param>
        /// <param name="exceptionBranch">
        /// The branch to take when an exception is thrown by an instruction
        /// in <paramref name="graph"/>. Instructions are not wrapped in
        /// 'try' flow if this parameter is set to <c>null</c>.
        /// </param>
        /// <returns>
        /// The tag of the imported graph's entry point.
        /// </returns>
        public BasicBlockTag Include(
            FlowGraph graph,
            Func<ReturnFlow, BasicBlockBuilder, BlockFlow> rewriteReturnFlow,
            Branch exceptionBranch)
        {
            // The first thing we want to do is compose a mapping of
            // value tags in `graph` to value tags in this
            // control-flow graph.
            var valueRenameMap = new Dictionary<ValueTag, ValueTag>();
            foreach (var insn in graph.NamedInstructions)
            {
                valueRenameMap[insn] = new ValueTag(insn.Tag.Name);
            }

            // Populate a basic block rename mapping.
            var blockMap = new Dictionary<BasicBlockTag, BasicBlockBuilder>();
            var blockRenameMap = new Dictionary<BasicBlockTag, BasicBlockTag>();
            foreach (var block in graph.BasicBlocks)
            {
                // Add a basic block.
                var newBlock = AddBasicBlock(block.Tag.Name);
                blockMap[block] = newBlock;
                blockRenameMap[block] = newBlock;

                // Also handle parameters here.
                foreach (var param in block.Parameters)
                {
                    var newParam = newBlock.AppendParameter(param.Type, param.Tag.Name);
                    valueRenameMap[param.Tag] = newParam.Tag;
                }
            }

            InstructionExceptionSpecs exceptionSpecs;
            if (exceptionBranch == null)
            {
                exceptionSpecs = null;
            }
            else
            {
                if (!graph.HasAnalysisFor<InstructionExceptionSpecs>())
                {
                    graph = graph.WithAnalysis(GetAnalysisFor<InstructionExceptionSpecs>());
                }
                exceptionSpecs = graph.GetAnalysisResult<InstructionExceptionSpecs>();
            }

            // Copy basic block instructions and flow.
            foreach (var block in graph.BasicBlocks)
            {
                var newBlock = blockMap[block];
                // Copy the block's instructions.
                foreach (var insn in block.NamedInstructions)
                {
                    if (exceptionBranch != null
                        && exceptionSpecs.GetExceptionSpecification(insn.Instruction).CanThrowSomething)
                    {
                        // Create a new block for the success path.
                        var successBlock = AddBasicBlock();
                        var successParam = successBlock.AppendParameter(insn.ResultType, valueRenameMap[insn]);

                        // Wrap the instruction in 'try' flow.
                        newBlock.Flow = new TryFlow(
                            insn.Instruction.MapArguments(valueRenameMap),
                            new Branch(successBlock, new[] { BranchArgument.TryResult }),
                            exceptionBranch);

                        // Update the current block.
                        newBlock = successBlock;
                    }
                    else
                    {
                        newBlock.AppendInstruction(
                            insn.Instruction.MapArguments(valueRenameMap),
                            valueRenameMap[insn]);
                    }
                }

                // If the block ends in 'return' flow, then we want to
                // turn that return into a jump to the continuation.
                if (block.Flow is ReturnFlow)
                {
                    var returnFlow = (ReturnFlow)block.Flow;
                    newBlock.Flow = rewriteReturnFlow(
                        new ReturnFlow(
                            returnFlow.ReturnValue.MapArguments(valueRenameMap)),
                        newBlock);
                }
                else
                {
                    newBlock.Flow = block.Flow
                        .MapValues(valueRenameMap)
                        .MapBlocks(blockRenameMap);
                }
            }

            return blockRenameMap[graph.EntryPointTag];
        }

        /// <summary>
        /// Applies a function to a copy of this control-flow graph and
        /// either incorporates those changes into this control-flow graph
        /// or discards them, depending on the Boolean value returned by the
        /// transforming function.
        /// </summary>
        /// <param name="transform">
        /// A function that takes a copy of this control-flow graph and modifies it.
        /// If the function returns <c>true</c>, then this control-flow graph is
        /// set to the modified version created by the function; otherwise, this
        /// control-flow graph is left unchanged.
        /// </param>
        /// <returns>
        /// <paramref name="transform"/>'s return value.
        /// </returns>
        public bool TryForkAndMerge(Func<FlowGraphBuilder, bool> transform)
        {
            var copy = new FlowGraphBuilder(ImmutableGraph);
            if (transform(copy))
            {
                ImmutableGraph = copy.ImmutableGraph;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
