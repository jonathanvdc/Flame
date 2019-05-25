using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// The "alloca to register" transformation, which tries
    /// to eliminate alloca instructions whose addresses do not
    /// escape.
    /// </summary>
    public sealed class AllocaToRegister : IntraproceduralOptimization
    {
        private AllocaToRegister()
        { }

        /// <summary>
        /// An instance of the alloca-to-register transform.
        /// </summary>
        public static readonly AllocaToRegister Instance = new AllocaToRegister();

        // This transform is based on the algorithm described by M. Braun et al
        // in Simple and Efficient Construction of Static Single Assignment Form
        // (https://pp.info.uni-karlsruhe.de/uploads/publikationen/braun13cc.pdf).

        /// <summary>
        /// Applies the alloca to register transformation to a flow graph.
        /// </summary>
        /// <param name="graph">
        /// A flow graph to transform.
        /// </param>
        /// <returns>
        /// A transformed flow graph.
        /// </returns>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // Our first order of business is to identify
            // all `alloca` instructions that we *cannot*
            // replace with SSA magic.
            //
            // To keep things simple, we'll just blacklist
            // any `alloca` that is used by instructions other
            // than `load` and `store`.
            var eligibleAllocas = GetAllocasWithoutIdentity(graph);

            // We now have a set of all blacklisted `alloca` instructions.
            // All we need to do now is actually apply the SSA construction
            // algorithm.
            var graphBuilder = graph.ToBuilder();
            var algo = new SSAConstructionAlgorithm(graphBuilder, eligibleAllocas);
            algo.Run();
            return graphBuilder.ToImmutable();
        }

        /// <summary>
        /// Finds all alloca instructions that do not have the identity
        /// property, that is, all alloca instructions that are used
        /// at least once by something that is neither a load or a store.
        /// </summary>
        /// <param name="graph">
        /// The flow graph to analyze.
        /// </param>
        /// <returns>
        /// The set of all alloca instructions without the identity property.
        /// </returns>
        private static HashSet<ValueTag> GetAllocasWithoutIdentity(FlowGraph graph)
        {
            var allocaInstructions = new HashSet<ValueTag>();
            var blacklistedAllocas = new HashSet<ValueTag>();

            // We will first look for instructions that *do* have the
            // identity property if they are allocas. Also, we will
            // compose a list of all alloca instructions.
            foreach (var selection in graph.NamedInstructions)
            {
                VisitForIdentityProperty(
                    selection.Instruction,
                    selection.Tag,
                    allocaInstructions,
                    blacklistedAllocas);
            }

            // Branch arguments always have the identity property.
            foreach (var block in graph.BasicBlocks)
            {
                var flow = block.Flow;
                foreach (var instruction in flow.Instructions)
                {
                    VisitForIdentityProperty(
                        instruction,
                        null,
                        allocaInstructions,
                        blacklistedAllocas);
                }

                foreach (var branch in flow.Branches)
                {
                    foreach (var arg in branch.Arguments)
                    {
                        if (arg.IsValue)
                        {
                            blacklistedAllocas.Add(arg.ValueOrNull);
                        }
                    }
                }
            }

            allocaInstructions.ExceptWith(blacklistedAllocas);
            return allocaInstructions;
        }

        private static void VisitForIdentityProperty(
            Instruction instruction,
            ValueTag tag,
            HashSet<ValueTag> allocaInstructions,
            HashSet<ValueTag> blacklistedAllocas)
        {
            var prototype = instruction.Prototype;
            if (prototype is LoadPrototype)
            {
                return;
            }
            else if (prototype is StorePrototype)
            {
                var storeProto = (StorePrototype)prototype;
                blacklistedAllocas.Add(storeProto.GetValue(instruction));
            }
            else
            {
                if (tag != null && prototype is AllocaPrototype)
                {
                    allocaInstructions.Add(tag);
                }

                blacklistedAllocas.UnionWith(instruction.Arguments);
            }
        }

        private struct SSAConstructionAlgorithm
        {
            public SSAConstructionAlgorithm(
                FlowGraphBuilder graphBuilder,
                HashSet<ValueTag> eligibleAllocas)
            {
                this.graphBuilder = graphBuilder;
                this.eligibleAllocas = eligibleAllocas;
                this.currentDef = new Dictionary<ValueTag, Dictionary<BasicBlockTag, ValueTag>>();
                this.incompletePhis = new Dictionary<BasicBlockTag, Dictionary<ValueTag, BlockParameter>>();
                this.filledBlocks = new HashSet<BasicBlockTag>();
                this.processedBlocks = new HashSet<BasicBlockTag>();
                this.predecessors = graphBuilder.GetAnalysisResult<BasicBlockPredecessors>();

                foreach (var alloca in eligibleAllocas)
                {
                    this.currentDef[alloca] = new Dictionary<BasicBlockTag, ValueTag>();
                }
                foreach (var tag in graphBuilder.BasicBlockTags)
                {
                    this.incompletePhis[tag] = new Dictionary<ValueTag, BlockParameter>();
                }
            }

            private FlowGraphBuilder graphBuilder;
            private HashSet<ValueTag> eligibleAllocas;
            private Dictionary<ValueTag, Dictionary<BasicBlockTag, ValueTag>> currentDef;
            private Dictionary<BasicBlockTag, Dictionary<ValueTag, BlockParameter>> incompletePhis;
            private HashSet<BasicBlockTag> filledBlocks;
            private HashSet<BasicBlockTag> processedBlocks;
            private BasicBlockPredecessors predecessors;

            /// <summary>
            /// Runs the SSA construction algorithm.
            /// </summary>
            public void Run()
            {
                var entryPoint = graphBuilder.GetBasicBlock(graphBuilder.EntryPointTag);

                // Create a set of all entry point parameters.
                // Here's why: entry point parameters have a
                // very specific meaning in Flame IR. Each parameter
                // corresponds to a method body parameter.
                // The SSA construction algorithm will sometimes
                // append block parameters to the entry point block,
                // which is not something we want.
                //
                // By keeping track of the original entry point
                // parameters, we can tell parameters inserted
                // by the algorithm from preexisting parameters.
                var oldEntryPointParams = entryPoint.Parameters;

                // Fill the entry point first. This will also
                // fill all blocks reachable from the entry
                // point.
                FillBlock(entryPoint);

                // Fill all the garbage blocks.
                foreach (var block in graphBuilder.BasicBlocks)
                {
                    FillBlock(block);
                }

                // Delete the allocas we replaced with copies.
                foreach (var tag in eligibleAllocas)
                {
                    graphBuilder.RemoveInstruction(tag);
                }

                // Somehow eliminate additional entry point parameters.
                // We will use one of two techniques, depending on the
                // situation:
                //
                //   1. If the entry point does not have any predecessors,
                //      then we will simply replace all newly created
                //      entry point parameters with `default` instructions.
                //
                //   2. If the entry point has one or more predecessors,
                //      then we'll create a new entry point block with
                //      a parameter list equivalent to the original entry
                //      point parameters and have that block jump to the
                //      original entry point.
                //
                var epPreds = predecessors.GetPredecessorsOf(entryPoint.Tag).ToArray();
                if (epPreds.Length == 0)
                {
                    var oldParamSet = new HashSet<ValueTag>(oldEntryPointParams.Select(p => p.Tag));
                    var newParams = entryPoint.Parameters;

                    // Restore entry point parameters.
                    entryPoint.Parameters = oldEntryPointParams;

                    // Define extra parameters as `default` constant instructions.
                    foreach (var parameter in newParams.Reverse())
                    {
                        if (!oldParamSet.Contains(parameter.Tag))
                        {
                            entryPoint.InsertInstruction(
                                0,
                                Instruction.CreateDefaultConstant(parameter.Type),
                                parameter.Tag);
                        }
                    }
                }
                else
                {
                    // Create a new entry point.
                    var newEntryPoint = graphBuilder.AddBasicBlock(
                        entryPoint.Tag.Name + ".thunk");

                    graphBuilder.EntryPointTag = newEntryPoint.Tag;

                    var oldToNew = new Dictionary<ValueTag, ValueTag>();
                    foreach (var oldParam in oldEntryPointParams)
                    {
                        var oldTag = oldParam.Tag;
                        var newTag = new ValueTag(oldTag.Name);
                        newEntryPoint.AppendParameter(
                            new BlockParameter(oldParam.Type, newTag));
                        oldToNew[oldTag] = newTag;
                    }

                    var branchArgs = new List<ValueTag>();
                    foreach (var parameter in entryPoint.Parameters)
                    {
                        ValueTag newTag;
                        if (oldToNew.TryGetValue(parameter.Tag, out newTag))
                        {
                            branchArgs.Add(newTag);
                        }
                        else
                        {
                            branchArgs.Add(
                                newEntryPoint.AppendInstruction(
                                    Instruction.CreateDefaultConstant(parameter.Type)));
                        }
                    }

                    newEntryPoint.Flow = new JumpFlow(entryPoint.Tag, branchArgs);
                }
            }

            /// <summary>
            /// Checks if the given block has been filled yet. A block is called
            /// 'filled' when the SSA construction has been applied to its
            /// instructions.
            /// </summary>
            private bool IsFilled(BasicBlockTag tag)
            {
                return filledBlocks.Contains(tag);
            }

            /// <summary>
            /// Checks if the given basic block can be sealed.
            /// </summary>
            private bool CanSealBlock(BasicBlockTag block)
            {
                foreach (var tag in predecessors.GetPredecessorsOf(block))
                {
                    if (!IsFilled(tag))
                    {
                        return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// Seals all blocks that can be sealed at this time.
            /// </summary>
            private void SealAllSealableBlocks()
            {
                foreach (var block in graphBuilder.BasicBlockTags)
                {
                    if (CanSealBlock(block))
                    {
                        SealBlock(block);
                    }
                }
            }

            /// <summary>
            /// Fills the given block.
            /// </summary>
            private void FillBlock(BasicBlockBuilder block)
            {
                if (!processedBlocks.Add(block.Tag))
                {
                    // Block is already being processed.
                    return;
                }

                // Try to seal the block right away if possible.
                if (CanSealBlock(block.Tag))
                {
                    SealBlock(block.Tag);
                }

                // Actually fill the block, starting with the block's instructions.
                foreach (var selection in block.NamedInstructions)
                {
                    Instruction newInstruction;
                    if (RewriteInstruction(selection.Instruction, block, out newInstruction))
                    {
                        selection.Instruction = newInstruction;
                    }
                }

                // Also process instructions in the block's flow.
                var newFlowInstructions = new List<Instruction>();
                bool flowChanged = false;
                foreach (var instruction in block.Flow.Instructions)
                {
                    Instruction newInstruction;
                    if (RewriteInstruction(instruction, block, out newInstruction))
                    {
                        flowChanged = true;
                        newFlowInstructions.Add(newInstruction);
                    }
                    else
                    {
                        newFlowInstructions.Add(instruction);
                    }
                }
                if (flowChanged)
                {
                    block.Flow = block.Flow.WithInstructions(newFlowInstructions);
                }

                // Declare the block to be filled.
                filledBlocks.Add(block.Tag);

                // Seal this block.
                SealAllSealableBlocks();

                // Fill successor blocks.
                foreach (var target in block.Flow.BranchTargets)
                {
                    FillBlock(graphBuilder.GetBasicBlock(target));
                }
            }

            /// <summary>
            /// Rewrites an instruction in a basic block.
            /// </summary>
            /// <param name="instruction">The instruction to rewrite.</param>
            /// <param name="block">The block that defines the instruction.</param>
            /// <param name="newInstruction">A rewritten instruction.</param>
            /// <returns>
            /// <c>true</c> if <paramref name="instruction"/> has been rewritten; otherwise <c>false</c>.
            /// </returns>
            private bool RewriteInstruction(
                Instruction instruction,
                BasicBlockTag block,
                out Instruction newInstruction)
            {
                var prototype = instruction.Prototype;
                if (prototype is LoadPrototype)
                {
                    var loadProto = (LoadPrototype)prototype;
                    var pointer = loadProto.GetPointer(instruction);
                    if (eligibleAllocas.Contains(pointer))
                    {
                        newInstruction = Instruction.CreateCopy(
                            instruction.ResultType,
                            ReadVariable(pointer, block, instruction.ResultType));
                        return true;
                    }
                }
                else if (prototype is StorePrototype)
                {
                    var storeProto = (StorePrototype)prototype;
                    var pointer = storeProto.GetPointer(instruction);
                    if (eligibleAllocas.Contains(pointer))
                    {
                        var value = storeProto.GetValue(instruction);
                        WriteVariable(pointer, block, value);
                        newInstruction = Instruction.CreateCopy(instruction.ResultType, value);
                        return true;
                    }
                }
                newInstruction = instruction;
                return false;
            }

            /// <summary>
            /// Seals the given block. A block can be sealed when all of its
            /// predecessors have been filled.
            /// </summary>
            private void SealBlock(BasicBlockTag block)
            {
                if (IsSealed(block))
                {
                    // Don't seal the same block twice.
                    return;
                }

                foreach (var pair in incompletePhis[block])
                {
                    // Resolve phi operands now.
                    AddPhiOperands(pair.Key, block, pair.Value.Tag, pair.Value.Type);
                }

                incompletePhis.Remove(block);
            }

            /// <summary>
            /// Tells if a block has been sealed yet.
            /// </summary>
            /// <param name="block">
            /// The block to check for sealedness.
            /// </param>
            /// <returns>
            /// <c>true</c> if the block has been sealed; otherwise, <c>false</c>.
            /// </returns>
            private bool IsSealed(BasicBlockTag block)
            {
                return !incompletePhis.ContainsKey(block);
            }

            /// <summary>
            /// States that a variable is written to.
            /// </summary>
            /// <param name="variable">
            /// The variable that is written to.
            /// </param>
            /// <param name="block">
            /// The block that writes to the variable.
            /// </param>
            /// <param name="value">
            /// The value assigned to the variable.
            /// </param>
            private void WriteVariable(ValueTag variable, BasicBlockTag block, ValueTag value)
            {
                currentDef[variable][block] = value;
            }

            /// <summary>
            /// Gets the value assigned to a variable.
            /// </summary>
            /// <param name="variable">
            /// The variable to read from.
            /// </param>
            /// <param name="block">
            /// The block that reads from the variable.
            /// </param>
            /// <param name="type">
            /// The variable's type.
            /// </param>
            /// <returns>
            /// The value stored in the variable.
            /// </returns>
            private ValueTag ReadVariable(
                ValueTag variable,
                BasicBlockTag block,
                IType type)
            {
                ValueTag value;
                if (currentDef[variable].TryGetValue(block, out value))
                {
                    return value;
                }
                else
                {
                    return ReadVariableRecursive(variable, block, type);
                }
            }

            private ValueTag ReadVariableRecursive(
                ValueTag variable,
                BasicBlockTag block,
                IType type)
            {
                if (!IsSealed(block))
                {
                    // The block reading the variable has not been
                    // sealed yet. That's fine. Just add an entry to
                    // the list of incomplete phis.
                    var val = new ValueTag(variable.Name + ".phi");
                    incompletePhis[block][variable] = new BlockParameter(type, val);
                    WriteVariable(variable, block, val);
                    return val;
                }

                var preds = predecessors.GetPredecessorsOf(block).ToArray();
                if (preds.Length == 1)
                {
                    // There's just one predecessor, so we definitely
                    // won't be needing a block parameter/phi here.
                    return ReadVariable(variable, preds[0], type);
                }
                else
                {
                    // Create a parameter/phi, then figure out what
                    // its arguments are.
                    var val = new ValueTag(variable.Name + ".phi");
                    WriteVariable(variable, block, val);
                    val = AddPhiOperands(variable, block, val, type);
                    WriteVariable(variable, block, val);
                    return val;
                }
            }

            private ValueTag AddPhiOperands(
                ValueTag variable,
                BasicBlockTag block,
                ValueTag phi,
                IType type)
            {
                // Define a block parameter.
                var blockBuilder = graphBuilder.GetBasicBlock(block);
                var blockParam = new BlockParameter(type, phi);
                blockBuilder.AppendParameter(blockParam);

                // Add an argument to all blocks that refer to the block.
                foreach (var pred in predecessors.GetPredecessorsOf(block))
                {
                    var predBlock = graphBuilder.GetBasicBlock(pred);
                    var modifiedBranches = new List<Branch>();
                    foreach (var branch in predBlock.Flow.Branches)
                    {
                        if (branch.Target == block)
                        {
                            modifiedBranches.Add(
                                branch.AddArgument(
                                    ReadVariable(variable, pred, type)));
                        }
                        else
                        {
                            modifiedBranches.Add(branch);
                        }
                    }
                    predBlock.Flow = predBlock.Flow.WithBranches(modifiedBranches);
                }

                // The original algorithm states that we should
                // remove trivial phis here, but maybe that's best
                // left to a separate copy propagation pass.
                return phi;
            }
        }
    }
}
