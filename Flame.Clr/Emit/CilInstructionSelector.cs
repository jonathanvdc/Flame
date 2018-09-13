using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Compiler.Target;
using Flame.Constants;
using Flame.TypeSystem;
using Mono.Cecil;
using CilInstruction = Mono.Cecil.Cil.Instruction;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using VariableDefinition = Mono.Cecil.Cil.VariableDefinition;

namespace Flame.Clr.Emit
{
    public sealed class CilInstructionSelector : ILinearInstructionSelector<CilCodegenInstruction>
    {
        /// <summary>
        /// Creates a CIL instruction selector.
        /// </summary>
        /// <param name="typeEnvironment">
        /// The type environment to use when selecting instructions.
        /// </param>
        /// <param name="allocaToVariableMapping">
        /// A mapping of `alloca` values to the local variables that
        /// are used as backing store for the `alloca`s.
        /// </param>
        public CilInstructionSelector(
            TypeEnvironment typeEnvironment,
            IReadOnlyDictionary<ValueTag, VariableDefinition> allocaToVariableMapping)
        {
            this.TypeEnvironment = typeEnvironment;
            this.AllocaToVariableMapping = allocaToVariableMapping;
        }

        /// <summary>
        /// Gets the type environment used by this CIL instruction selector.
        /// </summary>
        /// <value>The type environment.</value>
        public TypeEnvironment TypeEnvironment { get; private set; }

        /// <summary>
        /// Gets a mapping of `alloca` values to the local variables that
        /// are used as backing store for the `alloca`s.
        /// </summary>
        /// <value>A mapping of value tags to variable definitions.</value>
        public IReadOnlyDictionary<ValueTag, VariableDefinition> AllocaToVariableMapping { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<CilCodegenInstruction> CreateBlockMarker(BasicBlock block)
        {
            var results = new List<CilCodegenInstruction>();
            if (block.IsEntryPoint)
            {
                var preds = block.Graph.GetAnalysisResult<BasicBlockPredecessors>();
                if (preds.GetPredecessorsOf(block.Tag).Count() > 0)
                {
                    var tempTag = new BasicBlockTag(block.Tag.Name + ".preheader");
                    results.AddRange(CreateJumpTo(tempTag));
                    results.Add(new CilMarkTargetInstruction(block.Tag));
                    foreach (var tag in block.ParameterTags.Reverse())
                    {
                        results.Add(new CilStoreRegisterInstruction(tag));
                    }
                    results.Add(new CilMarkTargetInstruction(tempTag));
                }
                else
                {
                    results.Add(new CilMarkTargetInstruction(block.Tag));
                }
            }
            else
            {
                results.Add(new CilMarkTargetInstruction(block.Tag));
                foreach (var tag in block.ParameterTags.Reverse())
                {
                    results.Add(new CilStoreRegisterInstruction(tag));
                }
            }
            return results;
        }

        /// <inheritdoc/>
        public IReadOnlyList<CilCodegenInstruction> CreateJumpTo(BasicBlockTag target)
        {
            return new CilCodegenInstruction[]
            {
                new CilOpInstruction(
                    CilInstruction.Create(OpCodes.Br),
                    (insn, mapping) => insn.Operand = mapping[target])
            };
        }

        /// <inheritdoc/>
        public SelectedInstructions<CilCodegenInstruction> SelectInstructions(
            BlockFlow flow,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough)
        {
            if (flow is ReturnFlow)
            {
                var retFlow = (ReturnFlow)flow;
                var retValSelection = SelectInstructionsAndWrap(
                    retFlow.ReturnValue,
                    null,
                    graph);
                var insns = new List<CilCodegenInstruction>(retValSelection.Instructions);
                insns.Add(new CilOpInstruction(CilInstruction.Create(OpCodes.Ret)));
                fallthrough = null;
                return SelectedInstructions.Create(
                    insns,
                    retValSelection.Dependencies);
            }
            else if (flow is UnreachableFlow)
            {
                fallthrough = null;
                return SelectedInstructions.Create<CilCodegenInstruction>(
                    EmptyArray<CilCodegenInstruction>.Value,
                    EmptyArray<ValueTag>.Value);
            }
            else if (flow is JumpFlow)
            {
                var branch = ((JumpFlow)flow).Branch;
                fallthrough = branch.Target;
                return SelectBranchArguments(branch, graph);
            }
            throw new System.NotImplementedException();
        }

        private SelectedInstructions<CilCodegenInstruction> SelectBranchArguments(
            Branch branch,
            FlowGraph graph)
        {
            var instructions = new List<CilCodegenInstruction>();
            var dependencies = new HashSet<ValueTag>();
            foreach (var arg in branch.Arguments)
            {
                if (!arg.IsValue)
                {
                    throw new NotImplementedException(
                        $"Non-value argument '{arg}' is not supported yet.");
                }

                var argInsn = Instruction.CreateCopy(
                    graph.GetValueType(arg.ValueOrNull),
                    arg.ValueOrNull);

                var insnSelection = SelectInstructionsAndWrap(argInsn, null, graph);
                instructions.AddRange(insnSelection.Instructions);
                dependencies.UnionWith(insnSelection.Dependencies);
            }
            return SelectedInstructions.Create<CilCodegenInstruction>(
                instructions,
                dependencies.ToArray());
        }

        /// <inheritdoc/>
        public SelectedInstructions<CilCodegenInstruction> SelectInstructions(
            SelectedInstruction instruction)
        {
            VariableDefinition allocaVarDef;
            if (AllocaToVariableMapping.TryGetValue(instruction.Tag, out allocaVarDef))
            {
                return CreateSelection(CilInstruction.Create(OpCodes.Ldloca, allocaVarDef));
            }
            else
            {
                return SelectInstructionsAndWrap(
                    instruction.Instruction,
                    instruction.Tag,
                    instruction.Block.Graph);
            }
        }

        private static SelectedInstructions<CilCodegenInstruction> SelectInstructionsImpl(
            Instruction instruction,
            FlowGraph graph)
        {
            var proto = instruction.Prototype;
            if (proto is ConstantPrototype)
            {
                return CreateSelection(
                    CreatePushConstant(((ConstantPrototype)proto).Value));
            }
            else if (proto is CopyPrototype)
            {
                var copyProto = (CopyPrototype)proto;
                var copiedVal = copyProto.GetCopiedValue(instruction);
                if (graph.ContainsInstruction(copiedVal))
                {
                    var copiedInstruction = graph.GetInstruction(copiedVal).Instruction;
                    if (copiedInstruction.Prototype is ConstantPrototype
                        || copiedInstruction.Prototype is CopyPrototype)
                    {
                        // Always duplicate copies of constants to avoid
                        // wasting local variables on them.
                        return SelectInstructionsImpl(copiedInstruction, graph);
                    }
                }
                return SelectedInstructions.Create<CilCodegenInstruction>(
                    new CilCodegenInstruction[0],
                    new[] { copiedVal });
            }
            else if (proto is AllocaPrototype)
            {
                // TODO: turn `alloca` instructions that are not strictly
                // reachable from themselves into local variables.
                //
                // TODO: constant-fold `sizeof` whenever possible.
                var allocaProto = (AllocaPrototype)proto;
                return SelectedInstructions.Create<CilCodegenInstruction>(
                    new CilCodegenInstruction[]
                    {
                        new CilOpInstruction(
                            CilInstruction.Create(
                                OpCodes.Sizeof,
                                TypeHelpers.ToTypeReference(allocaProto.ElementType))),
                        new CilOpInstruction(CilInstruction.Create(OpCodes.Localloc))
                    },
                    new ValueTag[0]);
            }
            else if (proto is LoadPrototype)
            {
                var loadProto = (LoadPrototype)proto;
                var pointer = loadProto.GetPointer(instruction);
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Ldobj,
                        TypeHelpers.ToTypeReference(loadProto.ResultType)),
                    pointer);
            }
            else if (proto is StorePrototype)
            {
                var storeProto = (StorePrototype)proto;
                var pointer = storeProto.GetPointer(instruction);
                var value = storeProto.GetValue(instruction);
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Stobj,
                        TypeHelpers.ToTypeReference(storeProto.ResultType)),
                    pointer,
                    value);
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        private SelectedInstructions<CilCodegenInstruction> SelectInstructionsAndWrap(
            Instruction instruction,
            ValueTag tag,
            FlowGraph graph)
        {
            var impl = SelectInstructionsImpl(instruction, graph);

            var updatedInsns = new List<CilCodegenInstruction>();
            var updatedDependencies = new List<ValueTag>();
            // Load each dependency.
            foreach (var dependency in impl.Dependencies)
            {
                VariableDefinition allocaVarDef;
                if (AllocaToVariableMapping.TryGetValue(dependency, out allocaVarDef))
                {
                    updatedInsns.Add(
                        new CilOpInstruction(
                            CilInstruction.Create(OpCodes.Ldloca,
                            allocaVarDef)));
                }
                else
                {
                    updatedInsns.Add(new CilLoadRegisterInstruction(dependency));
                    updatedDependencies.Add(dependency);
                }
            }
            // Actually run the instructions.
            updatedInsns.AddRange(impl.Instructions);
            // Store the result if it's not a 'void' value.
            if (instruction.ResultType != TypeEnvironment.Void && tag != null)
            {
                updatedInsns.Add(new CilStoreRegisterInstruction(tag));
            }
            return SelectedInstructions.Create(updatedInsns, updatedDependencies);
        }

        private static CilInstruction CreatePushConstant(
            Constant constant)
        {
            if (constant is IntegerConstant)
            {
                var iconst = (IntegerConstant)constant;
                if (iconst.Spec.Size <= 32)
                {
                    return CilInstruction.Create(OpCodes.Ldc_I4, iconst.ToInt32());
                }
                else if (iconst.Spec.Size <= 64)
                {
                    return CilInstruction.Create(OpCodes.Ldc_I8, iconst.ToInt64());
                }
                else
                {
                    throw new NotSupportedException(
                        $"Integer constant '{constant}' cannot be emitted because it is " +
                        "too large to fit in a 64-bit integer.");
                }
            }
            else if (constant is BooleanConstant)
            {
                var bconst = (BooleanConstant)constant;
                return CilInstruction.Create(bconst.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            }
            else if (constant is NullConstant)
            {
                return CilInstruction.Create(OpCodes.Ldnull);
            }
            else if (constant is Float32Constant)
            {
                var fconst = (Float32Constant)constant;
                return CilInstruction.Create(OpCodes.Ldc_R4, fconst.Value);
            }
            else if (constant is Float64Constant)
            {
                var fconst = (Float64Constant)constant;
                return CilInstruction.Create(OpCodes.Ldc_R8, fconst.Value);
            }
            else if (constant is StringConstant)
            {
                var sconst = (Float64Constant)constant;
                return CilInstruction.Create(OpCodes.Ldstr, sconst.Value);
            }
            else
            {
                throw new NotSupportedException($"Unknown type of constant: '{constant}'.");
            }
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateSelection(
            CilInstruction instruction,
            params ValueTag[] dependencies)
        {
            return SelectedInstructions.Create<CilCodegenInstruction>(
                new CilOpInstruction(instruction),
                dependencies);
        }
    }
}
