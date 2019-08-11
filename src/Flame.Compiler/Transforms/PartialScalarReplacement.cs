using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Flame.Compiler.Analysis;
using Flame.Compiler.Instructions;
using Flame.Compiler.Instructions.Fused;
using Flame.Compiler.Pipeline;
using Flame.TypeSystem;

namespace Flame.Compiler.Transforms
{
    /// <summary>
    /// An optimization that replaces aggregates by scalars, i.e., their fields.
    /// Depending on the control-flow graph, aggregates might be replaced by
    /// scalars on some control-flow paths while remaining untouched on others.
    /// </summary>
    public sealed class PartialScalarReplacement : IntraproceduralOptimization
    {
        /// <summary>
        /// Creates a partial scalar replacement pass.
        /// </summary>
        /// <param name="canReplaceByScalars">
        /// Tells if a type is an aggregate that can be replaced by scalars.
        /// </param>
        public PartialScalarReplacement(Func<IType, bool> canReplaceByScalars)
        {
            this.CanReplaceByScalars = canReplaceByScalars;
        }

        /// <summary>
        /// An instance of the partial scalar replacement transform.
        /// </summary>
        /// <returns>A partial scalar replacement transform instance.</returns>
        public static readonly Optimization Instance
            = new AccessAwarePartialScalarReplacement();

        /// <summary>
        /// Tells if a particular type is an aggregate that can be replaced by scalars.
        /// </summary>
        /// <value>A predicate function.</value>
        public Func<IType, bool> CanReplaceByScalars { get; private set; }

        private sealed class AccessAwarePartialScalarReplacement : Optimization
        {
            public override bool IsCheckpoint => false;

            public override Task<MethodBody> ApplyAsync(MethodBody body, OptimizationState state)
            {
                var rules = body.Implementation.GetAnalysisResult<AccessRules>();
                var method = state.Method;
                var pass = new PartialScalarReplacement(
                    type =>
                        !type.IsPointerType()
                        && !type.IsSpecialType()
                        && !(type is IGenericParameter)
                        && type.GetAllInstanceFields()
                            .All(field => rules.CanAccess(method, field)));

                return Task.FromResult(body.WithImplementation(pass.Apply(body.Implementation)));
            }
        }

        /// <inheritdoc/>
        public override FlowGraph Apply(FlowGraph graph)
        {
            // This transform does the following things:
            //   1. It figures out which values need materializing.
            //   2. It delays the materialization of aggregates that cannot
            //      be fully replaced by scalars.
            //   3. It runs the regular scalar replacement pass.

            var builder = graph.ToBuilder();

            var analysisResults = new MaterializationAnalysis().Analyze(graph);
            DelayMaterialization(builder, analysisResults.BlockResults);
            builder.Transform(new ScalarReplacement(CanReplaceByScalars));

            return builder.ToImmutable();
        }

        private void DelayMaterialization(
            FlowGraphBuilder graph,
            IReadOnlyDictionary<BasicBlockTag, ImmutableHashSet<ValueTag>> materializationState)
        {
            // We now know where values need to be materialized. What we now want to do is
            // delay the materialization of aggregates as much as possible.

            var domTree = graph.GetAnalysisResult<DominatorTree>();
            var uses = graph.GetAnalysisResult<ValueUses>();

            // Find candidates for dematerialization and choose where to materialize them,
            // if anywhere.
            foreach (var candidate in GetDematerializationCandidates(graph))
            {
                var defBlock = graph.GetValueParent(candidate);
                BasicBlockTag materializationPoint;
                if (domTree.TryFindCommonDominator(
                    materializationState
                        .Where(pair => pair.Value.Contains(candidate))
                        .Select(pair => pair.Key)
                        .Where(tag => domTree.IsDominatedBy(tag, defBlock)),
                    out materializationPoint))
                {
                    if (materializationPoint == defBlock.Tag)
                    {
                        // Yeah, okay. This turned out to be a dead end.
                        continue;
                    }
                    else
                    {
                        // Replace all uses not dominated by the materialization point
                        // with a fully scalarrepl-able value. Replace all uses dominated
                        // by the materialization point with the materialized value, which
                        // we will insert at the start of the materialization point block.
                        var elementType = ((PointerType)graph.GetValueType(candidate)).ElementType;
                        var materializationBlock = graph.GetBasicBlock(materializationPoint);
                        var dematerializedValue = graph.EntryPoint.InsertInstruction(
                            0,
                            Instruction.CreateAlloca(elementType),
                            candidate.Name + ".demat");

                        var replacementMap = new Dictionary<ValueTag, ValueTag>()
                        {
                            { candidate, dematerializedValue }
                        };

                        // Rewrite uses in the dematerialized blocks.
                        foreach (var user in uses.GetInstructionUses(candidate))
                        {
                            var userInstruction = graph.GetInstruction(user);
                            if (!domTree.IsDominatedBy(userInstruction.Block, materializationPoint))
                            {
                                userInstruction.Instruction = userInstruction.Instruction.MapArguments(replacementMap);
                            }
                        }
                        foreach (var user in uses.GetFlowUses(candidate))
                        {
                            var block = graph.GetBasicBlock(user);
                            if (!domTree.IsDominatedBy(block, materializationPoint))
                            {
                                block.Flow = block.Flow.MapValues(replacementMap);
                            }
                        }

                        // Materialize the value by performing a fieldwise copy.
                        foreach (var field in elementType.GetAllInstanceFields().Reverse())
                        {
                            var dematerializedGFP = materializationBlock.InsertInstruction(
                                0,
                                Instruction.CreateGetFieldPointer(field, dematerializedValue));

                            var scalar = dematerializedGFP.InsertAfter(
                                Instruction.CreateLoad(field.FieldType, dematerializedGFP));

                            var candidateGFP = scalar.InsertAfter(
                                Instruction.CreateGetFieldPointer(field, candidate));

                            candidateGFP.InsertAfter(
                                Instruction.CreateStore(
                                    field.FieldType,
                                    candidateGFP,
                                    scalar));
                        }

                        // Usually sink the allocation.
                        NamedInstructionBuilder instruction;
                        if (graph.TryGetInstruction(candidate, out instruction)
                            && (!(instruction.Prototype is AllocaPrototype) || !defBlock.IsEntryPoint))
                        {
                            // Don't move entry-point `alloca` instructions around, but do delay
                            // all other instructions.
                            instruction.MoveTo(0, materializationBlock);
                        }
                    }
                }
                else
                {
                    // There's no point in doing anything here. The regular scarrepl
                    // pass (which is run by this pass) should be able to handle this
                    // situation: the value is never used in a way that warrants
                    // materialization.
                    continue;
                }
            }
        }

        private IEnumerable<ValueTag> GetDematerializationCandidates(
            FlowGraphBuilder graph)
        {
            foreach (var value in graph.ValueTags)
            {
                var type = graph.GetValueType(value) as PointerType;
                if (type == null || !CanReplaceByScalars(type.ElementType))
                {
                    continue;
                }

                // TODO: also consider heap-allocated values, for which partial scalar replacement
                // can be much more useful.
                NamedInstructionBuilder instruction;
                if (graph.TryGetInstruction(value, out instruction))
                {
                    if (instruction.Prototype is AllocaPrototype)
                    {
                        yield return instruction;
                    }
                }
                else
                {
                    yield return value;
                }
            }
        }

        private static ValueTag UnfoldAliases(ValueTag value, FlowGraph graph)
        {
            NamedInstruction def;
            if (graph.TryGetInstruction(value, out def))
            {
                var proto = def.Prototype;
                if (proto is CopyPrototype || proto is GetFieldPointerPrototype)
                {
                    return UnfoldAliases(def.Arguments[0], graph);
                }
            }
            return value;
        }

        /// <summary>
        /// A block fixpoint analysis that computes the set of all materialized
        /// values for a block.
        /// </summary>
        private sealed class MaterializationAnalysis : BlockFixpointAnalysis<ImmutableHashSet<ValueTag>>
        {
            public override ImmutableHashSet<ValueTag> CreateEntryPointInput(BasicBlock entryPoint)
            {
                // Parameters are always materialized.
                return ImmutableHashSet.CreateRange(entryPoint.ParameterTags);
            }

            public override bool Equals(ImmutableHashSet<ValueTag> first, ImmutableHashSet<ValueTag> second)
            {
                return first.SetEquals(second);
            }

            public override ImmutableHashSet<ValueTag> Merge(ImmutableHashSet<ValueTag> first, ImmutableHashSet<ValueTag> second)
            {
                return first.Union(second);
            }

            public override IEnumerable<KeyValuePair<BasicBlockTag, ImmutableHashSet<ValueTag>>> GetOutgoingInputs(
                BasicBlock block,
                ImmutableHashSet<ValueTag> output)
            {
                var domTree = block.Graph.GetAnalysisResult<DominatorTree>();
                var preds = block.Graph.GetAnalysisResult<BasicBlockPredecessors>();

                // Materialization is always propagated forward.
                var results = new List<KeyValuePair<BasicBlockTag, ImmutableHashSet<ValueTag>>>();
                foreach (var pair in base.GetOutgoingInputs(block, output))
                {
                    // Don't materialize values in blocks that are not dominated by the block
                    // that defines those values---those blocks can't use the value, so
                    // materializing values there doesn't make any sense.
                    var matVals = pair.Value
                        .Where(val =>
                            domTree.IsDominatedBy(
                                pair.Key,
                                block.Graph.GetValueParent(val)))
                        .ToImmutableHashSet();
                    results.Add(new KeyValuePair<BasicBlockTag, ImmutableHashSet<ValueTag>>(pair.Key, matVals));

                    // An edge from a block to another block that dominates said block indicates
                    // that we've encountered a loop. We absolutely don't want materialization
                    // to happen *inside* a loop. To avoid it, we'll propagate materialization
                    // information to all predecessors of the block.
                    foreach (var targetPred in preds.GetPredecessorsOf(pair.Key))
                    {
                        results.Add(new KeyValuePair<BasicBlockTag, ImmutableHashSet<ValueTag>>(pair.Key, matVals));
                    }
                }

                // Propagate materialization backward for basic block parameters.
                foreach (var pred in preds.GetPredecessorsOf(block))
                {
                    foreach (var branch in block.Graph.GetBasicBlock(pred).Flow.Branches)
                    {
                        if (branch.Target != block.Tag)
                        {
                            continue;
                        }

                        var imported = ImmutableHashSet.CreateBuilder<ValueTag>();
                        foreach (var pair in branch.ZipArgumentsWithParameters(block.Graph))
                        {
                            if (!output.Contains(pair.Key) || !pair.Value.IsValue)
                            {
                                continue;
                            }

                            imported.Add(pair.Value.ValueOrNull);
                        }
                        if (imported.Count > 0)
                        {
                            results.Add(
                                new KeyValuePair<BasicBlockTag, ImmutableHashSet<ValueTag>>(
                                    pred,
                                    imported.ToImmutable()));
                        }
                    }
                }

                // Propagate materialized basic block parameters all the way back to the
                // parameters' definitions.
                foreach (var value in output)
                {
                    if (block.Graph.ContainsBlockParameter(value))
                    {
                        results.Add(
                            new KeyValuePair<BasicBlockTag, ImmutableHashSet<ValueTag>>(
                                block.Graph.GetValueParent(value),
                                ImmutableHashSet.Create(value)));
                    }
                }

                return results;
            }

            public override ImmutableHashSet<ValueTag> Process(BasicBlock block, ImmutableHashSet<ValueTag> input)
            {
                var output = input.ToBuilder();

                // First, compute the materialization state of the basic block parameters.
                var preds = block.Graph.GetAnalysisResult<BasicBlockPredecessors>();
                foreach (var pred in preds.GetPredecessorsOf(block).Select(block.Graph.GetBasicBlock))
                {
                    foreach (var branch in pred.Flow.Branches)
                    {
                        if (branch.Target != block.Tag)
                        {
                            continue;
                        }

                        foreach (var pair in branch.ZipArgumentsWithParameters(block.Graph))
                        {
                            if (pair.Value.IsValue)
                            {
                                // A basic block parameter needs to be materialized if any of
                                // its arguments needs to be materialized.
                                if (output.Contains(UnfoldAliases(pair.Value.ValueOrNull, block.Graph)))
                                {
                                    output.Add(pair.Key);
                                }
                            }
                            else
                            {
                                // A basic block parameter needs to be materialized if any of
                                // its arguments are special.
                                output.Add(pair.Key);
                            }
                        }
                    }
                }

                // Visit the block's instructions and determine which values need to be
                // materialized.
                foreach (var instruction in block.NamedInstructions)
                {
                    UpdateMaterialization(
                        instruction.Instruction,
                        instruction,
                        block.Graph,
                        output);
                }

                // Ditto for the block's outgoing flow.
                foreach (var instruction in block.Flow.Instructions)
                {
                    UpdateMaterialization(
                        instruction,
                        null,
                        block.Graph,
                        output);
                }

                return output.ToImmutable();
            }

            private static void UpdateMaterialization(
                Instruction instruction,
                ValueTag tagOrNull,
                FlowGraph graph,
                ImmutableHashSet<ValueTag>.Builder materialization)
            {
                var proto = instruction.Prototype;
                if (proto is LoadPrototype)
                {
                    // Loads are fine; we can replace them without having to
                    // materialize values.
                }
                else if (proto is GetFieldPointerPrototype)
                {
                    // GFP instructions are harmless unless they escape.
                    if (tagOrNull == null)
                    {
                        materialization.UnionWith(
                            instruction.Arguments.Select(
                                arg => UnfoldAliases(arg, graph)));
                    }
                }
                else if (proto is StorePrototype)
                {
                    // Stores are usually okay, unless they're used to make a
                    // value escape. In that case, they're not okay.
                    materialization.Add(
                        UnfoldAliases(
                            ((StorePrototype)proto).GetValue(instruction),
                            graph));
                }
                else
                {
                    // All other instructions make values escape.
                    materialization.UnionWith(
                        instruction.Arguments.Select(
                            arg => UnfoldAliases(arg, graph)));
                }
            }
        }
    }
}
