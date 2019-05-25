using System.Collections.Generic;
using Flame.Collections;
using Flame.Compiler.Flow;
using System.Collections.Immutable;
using System;
using Flame.TypeSystem;
using System.Linq;
using Flame.Compiler.Analysis;
using Flame.Compiler.Transforms;

namespace Flame.Compiler
{
    /// <summary>
    /// An immutable control-flow graph that consists of basic blocks.
    /// </summary>
    public sealed class FlowGraph
    {
        /// <summary>
        /// Creates a control-flow graph that contains only an empty
        /// entry point block.
        /// </summary>
        public FlowGraph()
        {
            this.instructions = ImmutableDictionary.Create<ValueTag, Instruction>();
            this.blocks = ImmutableOrderedDictionary.Create<BasicBlockTag, BasicBlockData>();
            this.blockParamTypes = ImmutableDictionary.Create<ValueTag, IType>();
            this.valueParents = ImmutableDictionary.Create<ValueTag, BasicBlockTag>();
            this.analysisCache = new MacroAnalysisCache();
            this.EntryPointTag = new BasicBlockTag("entry-point");
            this.blocks = this.blocks.SetItem(
                this.EntryPointTag,
                new BasicBlockData());
        }

        private FlowGraph(
            FlowGraph other)
        {
            this.instructions = other.instructions;
            this.blocks = other.blocks;
            this.blockParamTypes = other.blockParamTypes;
            this.valueParents = other.valueParents;
            this.EntryPointTag = other.EntryPointTag;
            this.analysisCache = other.analysisCache;
        }

        private FlowGraph(
            FlowGraph other,
            FlowGraphUpdate update)
            : this(other)
        {
            this.analysisCache = other.analysisCache.Update(update);
        }

        private ImmutableDictionary<ValueTag, Instruction> instructions;
        private ImmutableOrderedDictionary<BasicBlockTag, BasicBlockData> blocks;
        private ImmutableDictionary<ValueTag, IType> blockParamTypes;
        private ImmutableDictionary<ValueTag, BasicBlockTag> valueParents;
        private MacroAnalysisCache analysisCache;

        /// <summary>
        /// Gets the tag of the entry point block.
        /// </summary>
        /// <returns>The tag of the entry point block.</returns>
        public BasicBlockTag EntryPointTag { get; private set; }

        /// <summary>
        /// Gets the entry point block.
        /// </summary>
        /// <returns>The entry point block.</returns>
        public BasicBlock EntryPoint => GetBasicBlock(EntryPointTag);

        /// <summary>
        /// Gets a sequence of all basic block tags in this control-flow graph.
        /// </summary>
        public IEnumerable<BasicBlockTag> BasicBlockTags => blocks.Keys;

        /// <summary>
        /// Gets a sequence of all basic blocks in this control-flow graph.
        /// </summary>
        /// <returns>All basic blocks.</returns>
        public IEnumerable<BasicBlock> BasicBlocks =>
            BasicBlockTags.Select(GetBasicBlock);

        /// <summary>
        /// Gets a sequence of all instruction tags in this control-flow graph.
        /// </summary>
        public IEnumerable<ValueTag> InstructionTags =>
            BasicBlocks.SelectMany(block => block.InstructionTags);

        /// <summary>
        /// Gets a sequence of all parameter tags in this control-flow graph.
        /// </summary>
        public IEnumerable<ValueTag> ParameterTags =>
            BasicBlocks.SelectMany(block => block.ParameterTags);

        /// <summary>
        /// Gets a sequence of all value tags in this control-flow graph.
        /// This sequence includes both instruction values and
        /// basic block parameter values.
        /// </summary>
        public IEnumerable<ValueTag> ValueTags =>
            InstructionTags
            .Concat(ParameterTags);

        /// <summary>
        /// Gets a sequence of all named instructions in this control-flow graph.
        /// Anonymous instructions as defined by block flow are not included.
        /// </summary>
        /// <returns>All named instructions</returns>
        public IEnumerable<NamedInstruction> NamedInstructions =>
            InstructionTags.Select(GetInstruction);

        /// <summary>
        /// Registers an analysis on this flow graph.
        /// </summary>
        /// <param name="analysis">The analysis to register.</param>
        /// <typeparam name="T">
        /// The type of result produced by the analysis.
        /// </typeparam>
        /// <returns>
        /// A new flow graph that includes the analysis.
        /// </returns>
        public FlowGraph WithAnalysis<T>(IFlowGraphAnalysis<T> analysis)
        {
            var newGraph = new FlowGraph(this);
            newGraph.analysisCache = analysisCache.WithAnalysis<T>(analysis);
            return newGraph;
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
            return analysisCache.GetResultAs<T>(this);
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
            return analysisCache.TryGetResultAs<T>(this, out result);
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
            return analysisCache.HasAnalysisFor<T>();
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
            return analysisCache.GetAnalysisFor<T>(this);
        }

        /// <summary>
        /// Creates a new basic block that includes all basic blocks in this
        /// graph plus an empty basic block. The latter basic block is returned.
        /// </summary>
        /// <param name="name">The (preferred) name of the basic block's tag.</param>
        /// <returns>An empty basic block in a new control-flow graph.</returns>
        public BasicBlock AddBasicBlock(string name)
        {
            var tag = new BasicBlockTag(name);
            var data = new BasicBlockData();

            var newGraph = new FlowGraph(this, new AddBasicBlockUpdate(tag));
            newGraph.blocks = newGraph.blocks.Add(tag, data);
            return new BasicBlock(newGraph, tag, data);
        }

        /// <summary>
        /// Creates a new basic block that includes all basic blocks in this
        /// graph plus an empty basic block. The latter basic block is returned.
        /// </summary>
        /// <returns>An empty basic block in a new control-flow graph.</returns>
        public BasicBlock AddBasicBlock()
        {
            return AddBasicBlock("");
        }

        /// <summary>
        /// Removes the basic block with a particular tag from this
        /// control-flow graph.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        /// <returns>
        /// A new control-flow graph that does not contain the basic block.
        /// </returns>
        public FlowGraph RemoveBasicBlock(BasicBlockTag tag)
        {
            AssertContainsBasicBlock(tag);

            var newGraph = new FlowGraph(this, new RemoveBasicBlockUpdate(tag));

            var oldData = blocks[tag];
            var oldParams = oldData.Parameters;
            var oldInsns = oldData.InstructionTags;

            var paramTypeBuilder = newGraph.blockParamTypes.ToBuilder();
            var valueParentBuilder = newGraph.valueParents.ToBuilder();

            int oldParamCount = oldParams.Count;
            for (int i = 0; i < oldParamCount; i++)
            {
                paramTypeBuilder.Remove(oldParams[i].Tag);
                valueParentBuilder.Remove(oldParams[i].Tag);
            }

            valueParentBuilder.RemoveRange(oldInsns);

            newGraph.blockParamTypes = paramTypeBuilder.ToImmutable();
            newGraph.valueParents = valueParentBuilder.ToImmutable();
            newGraph.instructions = newGraph.instructions.RemoveRange(oldInsns);
            newGraph.blocks = newGraph.blocks.Remove(tag);

            return newGraph;
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
        public FlowGraph RemoveInstruction(ValueTag instructionTag)
        {
            AssertContainsInstruction(instructionTag);
            var parentTag = valueParents[instructionTag];
            var oldBlockData = blocks[parentTag];
            var newBlockData = new BasicBlockData(
                oldBlockData.Parameters,
                oldBlockData.InstructionTags.Remove(instructionTag),
                oldBlockData.Flow);

            var newGraph = new FlowGraph(this, new RemoveInstructionUpdate(instructionTag));
            newGraph.blocks = newGraph.blocks.SetItem(parentTag, newBlockData);
            newGraph.instructions = newGraph.instructions.Remove(instructionTag);
            newGraph.valueParents = newGraph.valueParents.Remove(instructionTag);
            return newGraph;
        }

        /// <summary>
        /// Creates a new control-flow graph that takes the basic block
        /// with a particular tag as entry point.
        /// </summary>
        /// <param name="tag">The tag of the new entry point block.</param>
        /// <returns>A control-flow graph.</returns>
        public FlowGraph WithEntryPoint(BasicBlockTag tag)
        {
            AssertContainsBasicBlock(tag);
            var newGraph = new FlowGraph(this, new SetEntryPointUpdate(tag));
            newGraph.EntryPointTag = tag;
            return newGraph;
        }

        /// <summary>
        /// Gets the basic block with a particular tag.
        /// </summary>
        /// <param name="tag">The basic block's tag.</param>
        /// <returns>A basic block.</returns>
        public BasicBlock GetBasicBlock(BasicBlockTag tag)
        {
            AssertContainsBasicBlock(tag);
            return new BasicBlock(this, tag, blocks[tag]);
        }

        /// <summary>
        /// Gets the named instruction with a particular tag.
        /// </summary>
        /// <param name="tag">The instruction's tag.</param>
        /// <returns>A named instruction.</returns>
        public NamedInstruction GetInstruction(ValueTag tag)
        {
            AssertContainsInstruction(tag);
            return new NamedInstruction(
                GetValueParent(tag),
                tag,
                instructions[tag]);
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
        public bool TryGetInstruction(ValueTag tag, out NamedInstruction result)
        {
            Instruction insn;
            if (instructions.TryGetValue(tag, out insn))
            {
                result = new NamedInstruction(GetValueParent(tag), tag, insn);
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
            return blocks.ContainsKey(tag);
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
            return instructions.ContainsKey(tag);
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
            return blockParamTypes.ContainsKey(tag);
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
            return ContainsInstruction(tag)
                || ContainsBlockParameter(tag);
        }

        /// <summary>
        /// Gets the type of a value in this graph.
        /// </summary>
        /// <param name="tag">The value's tag.</param>
        /// <returns>The value's type.</returns>
        public IType GetValueType(ValueTag tag)
        {
            AssertContainsValue(tag);
            Instruction instr;
            if (instructions.TryGetValue(tag, out instr))
            {
                return instr.ResultType;
            }
            else
            {
                return blockParamTypes[tag];
            }
        }

        /// <summary>
        /// Gets basic block that defines a value with a
        /// particular tag.
        /// </summary>
        /// <param name="tag">The tag of the value to look for.</param>
        /// <returns>The basic block that defines the value.</returns>
        public BasicBlock GetValueParent(ValueTag tag)
        {
            AssertContainsValue(tag);
            return GetBasicBlock(valueParents[tag]);
        }

        /// <summary>
        /// Creates a mutable control-flow graph builder from
        /// this immutable control-flow graph.
        /// </summary>
        /// <returns>A mutable control-flow graph builder.</returns>
        public FlowGraphBuilder ToBuilder()
        {
            return new FlowGraphBuilder(this);
        }

        /// <summary>
        /// Asserts that this control-flow graph must contain a basic block
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the basic block that must be in the graph.
        /// </param>
        /// <param name="message">
        /// The error message for when no basic block in this control-flow graph
        /// has the tag.
        /// </param>
        public void AssertContainsBasicBlock(BasicBlockTag tag, string message)
        {
            ContractHelpers.Assert(ContainsBasicBlock(tag), message);
        }

        /// <summary>
        /// Asserts that this control-flow graph must contain a basic block
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the basic block that must be in the graph.
        /// </param>
        public void AssertContainsBasicBlock(BasicBlockTag tag)
        {
            AssertContainsBasicBlock(tag, "The graph does not contain the given basic block.");
        }

        /// <summary>
        /// Asserts that this control-flow graph must contain an instruction
        /// or basic block parameter with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the value that must be in the graph.
        /// </param>
        /// <param name="message">
        /// The error message for when no value in this control-flow graph
        /// has the tag.
        /// </param>
        public void AssertContainsValue(ValueTag tag, string message)
        {
            ContractHelpers.Assert(ContainsValue(tag), message);
        }

        /// <summary>
        /// Asserts that this control-flow graph must not contain an instruction
        /// or basic block parameter with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the value that must not be in the graph.
        /// </param>
        /// <param name="message">
        /// The error message for when a value in this control-flow graph
        /// has the tag.
        /// </param>
        public void AssertNotContainsValue(ValueTag tag, string message)
        {
            ContractHelpers.Assert(!ContainsValue(tag), message);
        }

        /// <summary>
        /// Asserts that this control-flow graph must contain an instruction
        /// or basic block parameter with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the value that must be in the graph.
        /// </param>
        public void AssertContainsValue(ValueTag tag)
        {
            AssertContainsValue(tag, "The graph does not contain the given value.");
        }

        /// <summary>
        /// Asserts that this control-flow graph must not contain an instruction
        /// or basic block parameter with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the value that must not be in the graph.
        /// </param>
        public void AssertNotContainsValue(ValueTag tag)
        {
            AssertNotContainsValue(tag, "The graph already contains a value with the given tag.");
        }

        /// <summary>
        /// Asserts that this control-flow graph must contain an instruction
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the instruction that must be in the graph.
        /// </param>
        /// <param name="message">
        /// The error message for when no instruction in this control-flow graph
        /// has the tag.
        /// </param>
        public void AssertContainsInstruction(ValueTag tag, string message)
        {
            ContractHelpers.Assert(ContainsInstruction(tag), message);
        }

        /// <summary>
        /// Asserts that this control-flow graph must contain an instruction
        /// with a particular tag.
        /// </summary>
        /// <param name="tag">
        /// The tag of the instruction that must be in the graph.
        /// </param>
        public void AssertContainsInstruction(ValueTag tag)
        {
            AssertContainsInstruction(tag, "The graph does not contain the given instruction.");
        }

        /// <summary>
        /// Applies a member mapping to this flow graph.
        /// </summary>
        /// <param name="mapping">A member mapping.</param>
        /// <returns>A transformed flow graph.</returns>
        public FlowGraph Map(MemberMapping mapping)
        {
            // Apply the mapping to all instructions.
            var newInstructionMap = ImmutableDictionary
                .Create<ValueTag, Instruction>()
                .ToBuilder();

            foreach (var insnPair in instructions)
            {
                newInstructionMap[insnPair.Key] = insnPair.Value.Map(mapping);
            }

            // Apply the mapping to all basic blocks.
            var newBlockMap = ImmutableOrderedDictionary
                .Create<BasicBlockTag, BasicBlockData>()
                .ToBuilder();

            var newParamTypeMap = ImmutableDictionary
                .Create<ValueTag, IType>()
                .ToBuilder();

            foreach (var blockPair in blocks)
            {
                var newBlock = blockPair.Value.Map(mapping);
                newBlockMap[blockPair.Key] = newBlock;
                foreach (var newBlockParam in newBlock.Parameters)
                {
                    newParamTypeMap[newBlockParam.Tag] = newBlockParam.Type;
                }
            }

            var result = new FlowGraph(this, new MapMembersUpdate(mapping));
            result.instructions = newInstructionMap.ToImmutable();
            result.blocks = newBlockMap.ToImmutable();
            result.blockParamTypes = newParamTypeMap.ToImmutable();
            return result;
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
        /// <returns>
        /// A new flow graph.
        /// </returns>
        public FlowGraph ReplaceUses(
            IReadOnlyDictionary<ValueTag, ValueTag> replacementMap)
        {
            var builder = ToBuilder();

            // Replace uses in instructions.
            foreach (var selection in builder.NamedInstructions)
            {
                Instruction newInstruction;
                if (TryReplaceUsesInInstruction(
                    selection.Instruction,
                    replacementMap,
                    out newInstruction))
                {
                    selection.Instruction = newInstruction;
                }
            }

            // Replace uses in flows.
            foreach (var block in builder.BasicBlocks)
            {
                var flow = block.Flow;
                BlockFlow newFlow;
                if (TryReplaceUsesInFlow(flow, replacementMap, out newFlow))
                {
                    block.Flow = newFlow;
                }
            }

            return builder.ToImmutable();
        }

        private static bool TryReplaceUsesInInstruction(
            Instruction instruction,
            IReadOnlyDictionary<ValueTag, ValueTag> replacementMap,
            out Instruction newInstruction)
        {
            var argCount = instruction.Arguments.Count;
            var newArgs = new ValueTag[argCount];
            bool replacedAny = false;
            for (int i = 0; i < argCount; i++)
            {
                var key = instruction.Arguments[i];
                ValueTag value;
                if (replacementMap.TryGetValue(key, out value)
                    && value != key)
                {
                    newArgs[i] = value;
                    replacedAny = true;
                }
                else
                {
                    newArgs[i] = key;
                }
            }
            if (replacedAny)
            {
                newInstruction = instruction.WithArguments(newArgs);
            }
            else
            {
                newInstruction = instruction;
            }
            return replacedAny;
        }

        private static bool TryReplaceUsesInFlow(
            BlockFlow flow,
            IReadOnlyDictionary<ValueTag, ValueTag> replacementMap,
            out BlockFlow newFlow)
        {
            bool replacedAny = false;
            var instructions = flow.Instructions;
            var newInstructions = new Instruction[instructions.Count];
            for (int i = 0; i < newInstructions.Length; i++)
            {
                Instruction newInsn;
                if (TryReplaceUsesInInstruction(instructions[i], replacementMap, out newInsn))
                {
                    newInstructions[i] = newInsn;
                    replacedAny = true;
                }
                else
                {
                    newInstructions[i] = instructions[i];
                }
            }

            var branches = flow.Branches;
            var newBranches = new Branch[branches.Count];
            for (int i = 0; i < newBranches.Length; i++)
            {
                var branch = branches[i];
                var newArgs = new BranchArgument[branch.Arguments.Count];
                for (int j = 0; j < newArgs.Length; j++)
                {
                    var arg = branch.Arguments[j];
                    ValueTag value;
                    if (arg.IsValue
                        && replacementMap.TryGetValue(arg.ValueOrNull, out value)
                        && value != arg.ValueOrNull)
                    {
                        newArgs[j] = BranchArgument.FromValue(value);
                        replacedAny = true;
                    }
                    else
                    {
                        newArgs[j] = arg;
                    }
                }
                newBranches[i] = branch.WithArguments(newArgs);
            }

            if (replacedAny)
            {
                newFlow = flow.WithBranches(newBranches).WithInstructions(newInstructions);
            }
            else
            {
                newFlow = flow;
            }
            return replacedAny;
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
        /// <returns>
        /// A flow graph that does not define any of the values.
        /// </returns>
        public FlowGraph RemoveDefinitions(IEnumerable<ValueTag> valuesToRemove)
        {
            var valuesToRemoveSet = new HashSet<ValueTag>(valuesToRemove);
            var graphBuilder = ToBuilder();
            foreach (var block in graphBuilder.BasicBlocks)
            {
                // Remove dead basic block parameters.
                block.Parameters = ImmutableList.CreateRange(
                    block.Parameters.Where(param => !valuesToRemoveSet.Contains(param.Tag)));

                // Remove arguments to deleted parameters.
                block.Flow = block.Flow.WithBranches(
                    block.Flow.Branches
                        .Select(branch =>
                            branch.WithArguments(
                                branch.ZipArgumentsWithParameters(this)
                                    .Where(pair => !valuesToRemoveSet.Contains(pair.Key))
                                    .Select(pair => pair.Value)
                                    .ToArray()))
                        .ToArray());
            }

            // Remove all dead instructions.
            foreach (var tag in graphBuilder.InstructionTags)
            {
                if (valuesToRemoveSet.Contains(tag))
                {
                    graphBuilder.RemoveInstruction(tag);
                }
            }

            return graphBuilder.ToImmutable();
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
        /// <returns>
        /// A flow graph that does not define any of the values.
        /// </returns>
        /// <remark>
        /// This method incurs less overhead than RemoveDefinitions but
        /// only works for instruction definitions, not for basic block
        /// parameter definitions.
        /// </remark>
        public FlowGraph RemoveInstructionDefinitions(IEnumerable<ValueTag> instructionsToRemove)
        {
            var result = this;
            foreach (var tag in instructionsToRemove)
            {
                if (result.ContainsInstruction(tag))
                {
                    result = result.RemoveInstruction(tag);
                }
                else if (result.ContainsBlockParameter(tag))
                {
                    // This is absolutely not allowed.
                    throw new ArgumentException(
                        $"'{nameof(RemoveInstructionDefinitions)}' can only remove instruction definitions, " +
                        $"but was asked to remove '{tag}', a basic block parameter; consider using " +
                        $"'{nameof(RemoveDefinitions)}' instead.");
                }
            }
            return result;
        }

        /// <summary>
        /// Applies an intraprocedural optimization to this flow graph.
        /// </summary>
        /// <param name="optimization">
        /// The transform to apply.
        /// </param>
        /// <returns>
        /// A transformed flow graph.
        /// </returns>
        public FlowGraph Transform(IntraproceduralOptimization optimization)
        {
            return optimization.Apply(this);
        }

        /// <summary>
        /// Applies a sequence of intraprocedural optimizations to this flow graph.
        /// </summary>
        /// <param name="optimizations">
        /// The transforms to apply.
        /// </param>
        /// <returns>
        /// A transformed flow graph.
        /// </returns>
        public FlowGraph Transform(IEnumerable<IntraproceduralOptimization> optimizations)
        {
            var result = this;
            foreach (var item in optimizations)
            {
                result = item.Apply(result);
            }
            return result;
        }

        /// <summary>
        /// Applies a sequence of intraprocedural optimizations to this flow graph.
        /// </summary>
        /// <param name="optimizations">
        /// The transforms to apply.
        /// </param>
        /// <returns>
        /// A transformed flow graph.
        /// </returns>
        public FlowGraph Transform(params IntraproceduralOptimization[] optimizations)
        {
            return Transform((IEnumerable<IntraproceduralOptimization>)optimizations);
        }

        internal BasicBlock UpdateBasicBlockFlow(BasicBlockTag tag, BlockFlow flow)
        {
            AssertContainsBasicBlock(tag);
            var oldBlock = blocks[tag];

            var newData = new BasicBlockData(
                oldBlock.Parameters,
                oldBlock.InstructionTags,
                flow);

            var newGraph = new FlowGraph(this, new BasicBlockFlowUpdate(tag));
            newGraph.blocks = newGraph.blocks.SetItem(tag, newData);

            return new BasicBlock(newGraph, tag, newData);
        }

        internal BasicBlock UpdateBasicBlockParameters(
            BasicBlockTag tag,
            ImmutableList<BlockParameter> parameters)
        {
            AssertContainsBasicBlock(tag);
            var oldBlock = blocks[tag];

            var newData = new BasicBlockData(
                parameters,
                oldBlock.InstructionTags,
                oldBlock.Flow);

            var oldData = blocks[tag];
            var oldParams = oldData.Parameters;

            var newGraph = new FlowGraph(this, new BasicBlockParametersUpdate(tag));

            var paramTypeBuilder = newGraph.blockParamTypes.ToBuilder();
            var valueParentBuilder = newGraph.valueParents.ToBuilder();

            // Remove the basic block's parameters from the value parent
            // and parameter type dictionaries.
            int oldParamCount = oldParams.Count;
            for (int i = 0; i < oldParamCount; i++)
            {
                paramTypeBuilder.Remove(oldParams[i].Tag);
                valueParentBuilder.Remove(oldParams[i].Tag);
            }

            // Add the new basic block parameters to the value parent and
            // parameter type dictionaries.
            int newParamCount = parameters.Count;
            for (int i = 0; i < newParamCount; i++)
            {
                var item = parameters[i];

                ContractHelpers.Assert(
                    !valueParentBuilder.ContainsKey(item.Tag),
                    "Value tag '" + item.Tag.Name + "' cannot appear twice in the same control-flow graph.");

                paramTypeBuilder.Add(item.Tag, item.Type);
                valueParentBuilder.Add(item.Tag, tag);
            }

            newGraph.blockParamTypes = paramTypeBuilder.ToImmutable();
            newGraph.valueParents = valueParentBuilder.ToImmutable();
            newGraph.blocks = newGraph.blocks.SetItem(tag, newData);

            return new BasicBlock(newGraph, tag, newData);
        }

        internal NamedInstruction InsertInstructionInBasicBlock(
            BasicBlockTag blockTag,
            Instruction instruction,
            ValueTag insnTag,
            int index)
        {
            AssertContainsBasicBlock(blockTag);
            AssertNotContainsValue(insnTag);

            var oldBlockData = blocks[blockTag];
            var newBlockData = new BasicBlockData(
                oldBlockData.Parameters,
                oldBlockData.InstructionTags.Insert(index, insnTag),
                oldBlockData.Flow);

            var newGraph = new FlowGraph(this, new AddInstructionUpdate(insnTag, instruction));
            newGraph.blocks = newGraph.blocks.SetItem(blockTag, newBlockData);
            newGraph.instructions = newGraph.instructions.Add(insnTag, instruction);
            newGraph.valueParents = newGraph.valueParents.Add(insnTag, blockTag);
            return new NamedInstruction(
                new BasicBlock(newGraph, blockTag, newBlockData),
                insnTag,
                instruction,
                index);
        }

        internal NamedInstruction ReplaceInstruction(ValueTag tag, Instruction instruction)
        {
            var newGraph = new FlowGraph(this, new ReplaceInstructionUpdate(tag, instruction));
            newGraph.instructions = newGraph.instructions.SetItem(tag, instruction);
            return new NamedInstruction(
                newGraph.GetValueParent(tag),
                tag,
                instruction);
        }
    }
}
