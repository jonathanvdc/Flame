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
using OpCode = Mono.Cecil.Cil.OpCode;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using VariableDefinition = Mono.Cecil.Cil.VariableDefinition;

namespace Flame.Clr.Emit
{
    /// <summary>
    /// An instruction selector for CIL codegen instructions.
    /// </summary>
    public sealed class CilInstructionSelector : ILinearInstructionSelector<CilCodegenInstruction>
    {
        /// <summary>
        /// Creates a CIL instruction selector.
        /// </summary>
        /// <param name="method">
        /// The method definition to select instructions for.
        /// </param>
        /// <param name="typeEnvironment">
        /// The type environment to use when selecting instructions.
        /// </param>
        /// <param name="allocaToVariableMapping">
        /// A mapping of `alloca` values to the local variables that
        /// are used as backing store for the `alloca`s.
        /// </param>
        public CilInstructionSelector(
            MethodDefinition method,
            TypeEnvironment typeEnvironment,
            IReadOnlyDictionary<ValueTag, VariableDefinition> allocaToVariableMapping)
        {
            this.Method = method;
            this.TypeEnvironment = typeEnvironment;
            this.AllocaToVariableMapping = allocaToVariableMapping;
            this.instructionOrder = new Dictionary<BasicBlockTag, LinkedList<ValueTag>>();
            this.selectedInstructions = new HashSet<ValueTag>();
            this.tempDefs = new List<VariableDefinition>();
            this.tempsByType = new Dictionary<IType, ValueTag>();
        }

        /// <summary>
        /// Gets the method definition to select instructions for.
        /// </summary>
        /// <value>A method definition.</value>
        public MethodDefinition Method { get; private set; }

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

        /// <summary>
        /// Gets a list of all temporaries defines by this instruction
        /// selector.
        /// </summary>
        public IReadOnlyList<VariableDefinition> Temporaries => tempDefs;

        private Dictionary<BasicBlockTag, LinkedList<ValueTag>> instructionOrder;
        private HashSet<ValueTag> selectedInstructions;
        private List<VariableDefinition> tempDefs;
        private Dictionary<IType, ValueTag> tempsByType;

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
                    CilInstruction.Create(OpCodes.Br, CilInstruction.Create(OpCodes.Nop)),
                    (insn, mapping) => insn.Operand = mapping[target])
            };
        }

        /// <inheritdoc/>
        public SelectedInstructions<CilCodegenInstruction> SelectInstructions(
            BlockFlow flow,
            BasicBlockTag blockTag,
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
                    blockTag,
                    GetInstructionList(graph.GetBasicBlock(blockTag)).Last,
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
                return SelectBranchArguments(
                    branch,
                    graph,
                    blockTag,
                    GetInstructionList(graph.GetBasicBlock(blockTag)).Last);
            }
            else if (flow is SwitchFlow)
            {
                return SelectForSwitchFlow(
                    (SwitchFlow)flow,
                    blockTag,
                    graph,
                    preferredFallthrough,
                    out fallthrough);
            }
            throw new System.NotImplementedException();
        }

        private SelectedInstructions<CilCodegenInstruction> SelectForSwitchFlow(
            SwitchFlow flow,
            BasicBlockTag blockTag,
            FlowGraph graph,
            BasicBlockTag preferredFallthrough,
            out BasicBlockTag fallthrough)
        {
            var switchFlow = (SwitchFlow)flow;

            var argValues = new HashSet<ValueTag>(
                switchFlow.Branches
                .SelectMany(branch => branch.Arguments)
                .Where(arg => arg.IsValue)
                .Select(arg => arg.ValueOrNull));

            var switchArgSelection = SelectInstructionsAndWrap(
                switchFlow.SwitchValue,
                null,
                blockTag,
                GetInstructionList(graph.GetBasicBlock(blockTag)).Last,
                graph,
                argValues);

            var instructions = new List<CilCodegenInstruction>(switchArgSelection.Instructions);
            var dependencies = new List<ValueTag>(switchArgSelection.Dependencies);

            if (switchFlow.IsIfElseFlow)
            {
                // If-else flow is fairly easy to handle.
                var ifBranch = switchFlow.Cases[0].Branch;
                var ifValue = switchFlow.Cases[0].Values.Single();

                bool hasArgs = ifBranch.Arguments.Count > 0;
                var ifTarget = hasArgs ? new BasicBlockTag() : ifBranch.Target;
                var elseTarget = switchFlow.DefaultBranch.Target;

                // Emit equality test.
                instructions.Add(new CilOpInstruction(CreatePushConstant(ifValue)));
                instructions.Add(
                    new CilOpInstruction(
                        CilInstruction.Create(
                            hasArgs ? OpCodes.Bne_Un : OpCodes.Beq,
                            CilInstruction.Create(OpCodes.Nop)),
                        (insn, branchTargets) => insn.Operand = branchTargets[hasArgs ? elseTarget : ifTarget]));

                // If the if-branch takes one or more arguments, then we need to
                // build a little thunk that selects instructions for those arguments.
                if (hasArgs)
                {
                    instructions.Add(new CilMarkTargetInstruction(ifTarget));
                    var ifArgs = SelectBranchArguments(ifBranch, graph);
                    instructions.AddRange(ifArgs.Instructions);
                    dependencies.AddRange(ifArgs.Dependencies);
                    instructions.Add(
                        new CilOpInstruction(
                            CilInstruction.Create(
                                OpCodes.Br,
                                CilInstruction.Create(OpCodes.Nop)),
                            (insn, branchTargets) => insn.Operand = branchTargets[ifBranch.Target]));
                }

                // Emit branch arguments for fallthrough branch.
                var defaultArgs = SelectBranchArguments(switchFlow.DefaultBranch, graph);
                instructions.AddRange(defaultArgs.Instructions);
                dependencies.AddRange(defaultArgs.Dependencies);
                fallthrough = elseTarget;
                return SelectedInstructions.Create(instructions, dependencies);
            }
            else if (flow.IsJumpTable)
            {
                bool defaultHasArguments = switchFlow.DefaultBranch.Arguments.Count != 0;
                var defaultTarget = defaultHasArguments
                    ? new BasicBlockTag()
                    : switchFlow.DefaultBranch.Target;

                // Create a sorted list of (value, switch target) pairs.
                var switchTargets = new List<KeyValuePair<IntegerConstant, BasicBlockTag>>();
                foreach (var switchCase in flow.Cases)
                {
                    foreach (var value in switchCase.Values)
                    {
                        switchTargets.Add(
                            new KeyValuePair<IntegerConstant, BasicBlockTag>(
                                (IntegerConstant)value,
                                switchCase.Branch.Target));
                    }
                }
                switchTargets.Sort((first, second) => first.Key.CompareTo(second.Key));

                // Figure out what the min and max switch target values are.
                var minValue = switchTargets.Count == 0
                    ? new IntegerConstant(0)
                    : switchTargets[0].Key;

                var maxValue = switchTargets.Count == 0
                    ? new IntegerConstant(0)
                    : switchTargets[switchTargets.Count - 1].Key;

                // Compose a list of switch targets.
                var targetList = new BasicBlockTag[maxValue.Subtract(minValue).ToInt32() + 1];
                foreach (var pair in switchTargets)
                {
                    targetList[pair.Key.Subtract(minValue).ToInt32()] = pair.Value;
                }
                for (int i = 0; i < targetList.Length; i++)
                {
                    if (targetList[i] == null)
                    {
                        targetList[i] = defaultTarget;
                    }
                }

                // Tweak the value being switched on if necessary.
                if (!minValue.IsZero)
                {
                    instructions.Add(new CilOpInstruction(CreatePushConstant(minValue)));
                    instructions.Add(new CilOpInstruction(OpCodes.Sub));
                }

                // Generate the actual switch instruction.
                instructions.Add(
                    new CilOpInstruction(
                        CilInstruction.Create(OpCodes.Switch, new CilInstruction[0]),
                        (insn, branchTargets) =>
                            insn.Operand = targetList.Select(target => branchTargets[target]).ToArray()));

                // Select branch arguments if the default branch has any.
                if (defaultHasArguments)
                {
                    instructions.Add(new CilMarkTargetInstruction(defaultTarget));
                    var defaultBranchSelection = SelectBranchArguments(
                        switchFlow.DefaultBranch,
                        graph);
                    instructions.AddRange(defaultBranchSelection.Instructions);
                    dependencies.AddRange(defaultBranchSelection.Dependencies);
                }
                fallthrough = switchFlow.DefaultBranch.Target;
                return SelectedInstructions.Create(instructions, dependencies);
            }
            else
            {
                throw new NotSupportedException(
                    "Only if-else and jump table switches are supported. " +
                    "Rewrite other switches prior to instruction selection.");
            }
        }

        private SelectedInstructions<CilCodegenInstruction> SelectBranchArguments(
            Branch branch,
            FlowGraph graph,
            BasicBlockTag blockTag = null,
            LinkedListNode<ValueTag> insertionPoint = null)
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

                var insnSelection = SelectInstructionsAndWrap(argInsn, null, blockTag, insertionPoint, graph);
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
            if (!selectedInstructions.Add (instruction.Tag))
            {
                // Never ever re-select instructions that have already
                // been selected inline.
                return new SelectedInstructions<CilCodegenInstruction>(
                    EmptyArray<CilCodegenInstruction>.Value,
                    EmptyArray<ValueTag>.Value);
            }

            VariableDefinition allocaVarDef;
            if (AllocaToVariableMapping.TryGetValue(instruction.Tag, out allocaVarDef))
            {
                return CreateSelection(CilInstruction.Create(OpCodes.Ldloca, allocaVarDef));
            }
            else if (IsDefaultConstant(instruction.Instruction))
            {
                // 'Default' constants are special.
                var resultType = instruction.Instruction.ResultType;
                if (resultType.Equals(TypeEnvironment.Void))
                {
                    return new SelectedInstructions<CilCodegenInstruction>(
                        EmptyArray<CilCodegenInstruction>.Value,
                        EmptyArray<ValueTag>.Value);
                }
                else
                {
                    var importedType = Method.Module.ImportReference(resultType);
                    return new SelectedInstructions<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilAddressOfRegisterInstruction(instruction.Tag),
                            new CilOpInstruction(
                                CilInstruction.Create(
                                    OpCodes.Initobj,
                                    importedType))
                        },
                        EmptyArray<ValueTag>.Value);
                }
            }
            else
            {
                var block = instruction.Block;
                return SelectInstructionsAndWrap(
                    instruction.Instruction,
                    instruction.Tag,
                    block.Tag,
                    GetInstructionNode(instruction.Tag, block.Graph),
                    block.Graph);
            }
        }

        private SelectedInstructions<CilCodegenInstruction> SelectInstructionsImpl(
            Instruction instruction,
            FlowGraph graph)
        {
            var proto = instruction.Prototype;
            if (proto is ConstantPrototype)
            {
                if (proto.ResultType == TypeEnvironment.Void)
                {
                    return CreateNopSelection(EmptyArray<ValueTag>.Value);
                }
                else
                {
                    return CreateSelection(
                        CreatePushConstant(((ConstantPrototype)proto).Value));
                }
            }
            else if (proto is CopyPrototype)
            {
                return CreateNopSelection(instruction.Arguments);
            }
            else if (proto is ReinterpretCastPrototype)
            {
                // Do nothing.
                // TODO: should we maybe sometimes emit castclass opcodes
                // to ensure that the bytecode we emit stays verifiable
                // even when we know that downcast checks can be elided?
                return CreateNopSelection(instruction.Arguments);
            }
            else if (proto is BoxPrototype)
            {
                var boxProto = (BoxPrototype)proto;
                return CreateSelection(
                    CilInstruction.Create(
                        OpCodes.Box,
                        Method.Module.ImportReference(boxProto.ElementType)),
                    instruction.Arguments);
            }
            else if (proto is AllocaPrototype)
            {
                // TODO: constant-fold `sizeof` whenever possible.
                var allocaProto = (AllocaPrototype)proto;
                return SelectedInstructions.Create<CilCodegenInstruction>(
                    new CilCodegenInstruction[]
                    {
                        new CilOpInstruction(
                            CilInstruction.Create(
                                OpCodes.Sizeof,
                                Method.Module.ImportReference(allocaProto.ElementType))),
                        new CilOpInstruction(CilInstruction.Create(OpCodes.Localloc))
                    },
                    new ValueTag[0]);
            }
            else if (proto is LoadPrototype)
            {
                var loadProto = (LoadPrototype)proto;
                var pointer = loadProto.GetPointer(instruction);
                VariableDefinition allocaVarDef;
                if (AllocaToVariableMapping.TryGetValue(pointer, out allocaVarDef))
                {
                    return CreateSelection(CilInstruction.Create(OpCodes.Ldloc, allocaVarDef));
                }
                else
                {
                    return CreateSelection(
                        CilInstruction.Create(
                            OpCodes.Ldobj,
                            Method.Module.ImportReference(loadProto.ResultType)),
                        pointer);
                }
            }
            else if (proto is StorePrototype)
            {
                var storeProto = (StorePrototype)proto;
                var pointer = storeProto.GetPointer(instruction);
                var value = storeProto.GetValue(instruction);
                VariableDefinition allocaVarDef;
                if (AllocaToVariableMapping.TryGetValue(pointer, out allocaVarDef))
                {
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                            new CilOpInstruction(
                                CilInstruction.Create(OpCodes.Stloc, allocaVarDef))
                        },
                        new[] { value });
                }
                else
                {
                    return SelectedInstructions.Create<CilCodegenInstruction>(
                        new CilCodegenInstruction[]
                        {
                            new CilOpInstruction(CilInstruction.Create(OpCodes.Dup)),
                            new CilOpInstruction(
                                CilInstruction.Create(
                                    OpCodes.Stobj,
                                    Method.Module.ImportReference(storeProto.ResultType)))
                        },
                        new[] { pointer, value });
                }
            }
            else if (proto is IntrinsicPrototype)
            {
                var intrinsicProto = (IntrinsicPrototype)proto;
                return SelectForIntrinsic(intrinsicProto, instruction.Arguments);
            }
            else if (proto is CallPrototype)
            {
                var callProto = (CallPrototype)proto;
                var dependencies = new List<ValueTag>();
                if (!callProto.Callee.IsStatic)
                {
                    dependencies.Add(callProto.GetThisArgument(instruction));
                }
                dependencies.AddRange(callProto.GetArgumentList(instruction).ToArray());
                var instructions = new[]
                {
                    new CilOpInstruction(
                        CilInstruction.Create(
                            callProto.Lookup == MethodLookup.Virtual
                            ? OpCodes.Callvirt
                            : OpCodes.Call,
                            Method.Module.ImportReference(callProto.Callee)))
                };
                return new SelectedInstructions<CilCodegenInstruction>(instructions, dependencies);
            }
            else
            {
                throw new NotImplementedException("Unknown instruction type: " + proto);
            }
        }

        /// <summary>
        /// Selects instructions for an intrinsic.
        /// </summary>
        /// <param name="prototype">
        /// The intrinsic prototype to select instructions for.
        /// </param>
        /// <param name="arguments">
        /// The intrinsic's list of arguments.
        /// </param>
        /// <returns>
        /// A batch of selected instructions for the intrinsic.
        /// </returns>
        private SelectedInstructions<CilCodegenInstruction> SelectForIntrinsic(
            IntrinsicPrototype prototype,
            IReadOnlyList<ValueTag> arguments)
        {
            string opName;
            if (ArithmeticIntrinsics.TryParseArithmeticIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (prototype.ParameterCount == 1)
                {
                    // There are only a few unary arithmetic intrinsics and
                    // they're all special.
                    var paramType = prototype.ParameterTypes[0];
                    if (opName == ArithmeticIntrinsics.Operators.Not)
                    {
                        // The 'arith.not' intrinsic can be implemented in one
                        // of three ways:
                        //
                        //   1. As an actual `not` instruction. This works for
                        //      signed integers, UInt32 and UInt64.
                        //
                        //   2. As a `ldc.i4.0; ceq` sequence. This works for
                        //      Booleans only.
                        //
                        //   3. As a `xor` with the all-ones pattern for the
                        //      integer type. This works for all unsigned integers.
                        //
                        var intSpec = paramType.GetIntegerSpecOrNull();
                        if (intSpec.Size == 32 || intSpec.Size == 64 || intSpec.IsSigned)
                        {
                            return CreateSelection(
                                CilInstruction.Create(OpCodes.Not),
                                arguments);
                        }
                        else if (intSpec.Size == 1)
                        {
                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                new[]
                                {
                                    new CilOpInstruction(OpCodes.Ldc_I4_0),
                                    new CilOpInstruction(OpCodes.Ceq)
                                },
                                arguments);
                        }
                        else
                        {
                            var allOnes = intSpec.UnsignedModulus - 1;
                            var allOnesConst = new IntegerConstant(
                                allOnes,
                                intSpec.UnsignedVariant);
                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                new[]
                                {
                                    new CilOpInstruction(CreatePushConstant(allOnesConst)),
                                    new CilOpInstruction(OpCodes.Xor)
                                },
                                arguments);
                        }
                    }
                    else if (opName == ArithmeticIntrinsics.Operators.Convert)
                    {
                        // Conversions are interesting because Flame IR has a much
                        // richer type system than the CIL stack type system. Hence,
                        // integer zext/sext is typically unnecessary. The code
                        // below takes advantage of that fact to reduce the number
                        // of instructions emitted.

                        if (paramType == prototype.ResultType)
                        {
                            // Do nothing.
                            return CreateNopSelection(arguments);
                        }
                        else if (paramType == TypeEnvironment.Float32 || paramType == TypeEnvironment.Float64)
                        {
                            var instructions = new List<CilCodegenInstruction>();
                            if (paramType.IsUnsignedIntegerType())
                            {
                                instructions.Add(new CilOpInstruction(OpCodes.Conv_R_Un));
                            }
                            instructions.Add(
                                new CilOpInstruction(
                                    paramType == TypeEnvironment.Float32
                                    ? OpCodes.Conv_R4
                                    : OpCodes.Conv_R8));
                            return SelectedInstructions.Create<CilCodegenInstruction>(
                                instructions,
                                arguments);
                        }

                        var targetSpec = prototype.ResultType.GetIntegerSpecOrNull();

                        if (targetSpec != null)
                        {
                            var sourceSpec = paramType.GetIntegerSpecOrNull();
                            if (sourceSpec != null)
                            {
                                if (sourceSpec.Size == targetSpec.Size)
                                {
                                    // Sign conversions are really just no-ops.
                                    return CreateNopSelection(arguments);
                                }
                                else if (sourceSpec.Size <= targetSpec.Size
                                    && targetSpec.Size <= 32)
                                {
                                    // Integers smaller than 32 bits are represented as
                                    // 32-bit integers on the stack, so we can just do
                                    // nothing here.
                                    return CreateNopSelection(arguments);
                                }
                            }

                            // Use dedicated opcodes for conversion to common
                            // integer types.
                            if (targetSpec.IsSigned)
                            {
                                if (targetSpec.Size == 8)
                                {
                                    return CreateSelection(OpCodes.Conv_I1, arguments);
                                }
                                else if (targetSpec.Size == 16)
                                {
                                    return CreateSelection(OpCodes.Conv_I2, arguments);
                                }
                                else if (targetSpec.Size == 32)
                                {
                                    return CreateSelection(OpCodes.Conv_I4, arguments);
                                }
                                else if (targetSpec.Size == 64)
                                {
                                    return CreateSelection(OpCodes.Conv_I8, arguments);
                                }
                            }
                            else
                            {
                                if (targetSpec.Size == 8)
                                {
                                    return CreateSelection(OpCodes.Conv_U1, arguments);
                                }
                                else if (targetSpec.Size == 16)
                                {
                                    return CreateSelection(OpCodes.Conv_U2, arguments);
                                }
                                else if (targetSpec.Size == 32)
                                {
                                    return CreateSelection(OpCodes.Conv_U4, arguments);
                                }
                                else if (targetSpec.Size == 64)
                                {
                                    return CreateSelection(OpCodes.Conv_U8, arguments);
                                }
                            }

                            if (targetSpec.Size == 1)
                            {
                                // There's no dedicated opcode for converting values
                                // to 1-bit integers (Booleans), so we'll just extract
                                // the least significant bit.
                                var instructions = new List<CilCodegenInstruction>();
                                if (sourceSpec == null)
                                {
                                    instructions.Add(new CilOpInstruction(OpCodes.Conv_I4));
                                }
                                instructions.AddRange(new[]
                                {
                                    new CilOpInstruction(OpCodes.Ldc_I4_1),
                                    new CilOpInstruction(OpCodes.And)
                                });
                                return SelectedInstructions.Create<CilCodegenInstruction>(
                                    instructions,
                                    arguments);
                            }
                        }

                        throw new NotSupportedException(
                            $"Unsupported primitive conversion of '{paramType}' to '{prototype.ResultType}'.");
                    }
                }
                else if (prototype.ParameterCount == 2)
                {
                    OpCode[] cilOps;
                    if ((prototype.ParameterTypes[0].IsUnsignedIntegerType()
                            && unsignedArithmeticBinaries.TryGetValue(opName, out cilOps))
                        || signedArithmeticBinaries.TryGetValue(opName, out cilOps))
                    {
                        return SelectedInstructions.Create<CilCodegenInstruction>(
                            cilOps.EagerSelect(op => new CilOpInstruction(op)),
                            arguments);
                    }
                }
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown arithmetic intrinsic '{prototype.Name}'.");
            }
            else if (ObjectIntrinsics.Namespace.TryParseIntrinsicName(
                prototype.Name,
                out opName))
            {
                if (opName == ObjectIntrinsics.Operators.UnboxAny
                    && prototype.ParameterCount == 1)
                {
                    return CreateSelection(
                        CilInstruction.Create(
                            OpCodes.Unbox_Any,
                            Method.Module.ImportReference(prototype.ResultType)),
                        arguments);
                }
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown object-oriented intrinsic '{prototype.Name}'.");
            }
            else
            {
                throw new NotSupportedException(
                    $"Cannot select instructions for call to unknown intrinsic '{prototype.Name}'.");
            }
        }

        private static Dictionary<string, OpCode[]> signedArithmeticBinaries =
            new Dictionary<string, OpCode[]>()
        {
            { ArithmeticIntrinsics.Operators.Add, new[] { OpCodes.Add } },
            { ArithmeticIntrinsics.Operators.Subtract, new[] { OpCodes.Sub } },
            { ArithmeticIntrinsics.Operators.Multiply, new[] { OpCodes.Mul } },
            { ArithmeticIntrinsics.Operators.Divide, new[] { OpCodes.Div } },
            { ArithmeticIntrinsics.Operators.Remainder, new[] { OpCodes.Rem } },
            { ArithmeticIntrinsics.Operators.IsEqualTo, new[] { OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsNotEqualTo, new[] { OpCodes.Ceq, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsLessThan, new[] { OpCodes.Clt } },
            { ArithmeticIntrinsics.Operators.IsGreaterThanOrEqualTo, new[] { OpCodes.Clt, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsGreaterThan, new[] { OpCodes.Cgt } },
            { ArithmeticIntrinsics.Operators.IsLessThanOrEqualTo, new[] { OpCodes.Cgt, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.And, new[] { OpCodes.And } },
            { ArithmeticIntrinsics.Operators.Or, new[] { OpCodes.Or } },
            { ArithmeticIntrinsics.Operators.Xor, new[] { OpCodes.Xor } },
            { ArithmeticIntrinsics.Operators.LeftShift, new[] { OpCodes.Shl } },
            { ArithmeticIntrinsics.Operators.RightShift, new[] { OpCodes.Shr } }
        };

        private static Dictionary<string, OpCode[]> unsignedArithmeticBinaries =
            new Dictionary<string, OpCode[]>()
        {
            { ArithmeticIntrinsics.Operators.Add, new[] { OpCodes.Add } },
            { ArithmeticIntrinsics.Operators.Subtract, new[] { OpCodes.Sub } },
            { ArithmeticIntrinsics.Operators.Multiply, new[] { OpCodes.Mul } },
            { ArithmeticIntrinsics.Operators.Divide, new[] { OpCodes.Div_Un } },
            { ArithmeticIntrinsics.Operators.Remainder, new[] { OpCodes.Rem_Un } },
            { ArithmeticIntrinsics.Operators.IsEqualTo, new[] { OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsNotEqualTo, new[] { OpCodes.Ceq, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsLessThan, new[] { OpCodes.Clt_Un } },
            { ArithmeticIntrinsics.Operators.IsGreaterThanOrEqualTo, new[] { OpCodes.Clt_Un, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.IsGreaterThan, new[] { OpCodes.Cgt_Un } },
            { ArithmeticIntrinsics.Operators.IsLessThanOrEqualTo, new[] { OpCodes.Cgt_Un, OpCodes.Ldc_I4_0, OpCodes.Ceq } },
            { ArithmeticIntrinsics.Operators.And, new[] { OpCodes.And } },
            { ArithmeticIntrinsics.Operators.Or, new[] { OpCodes.Or } },
            { ArithmeticIntrinsics.Operators.Xor, new[] { OpCodes.Xor } },
            { ArithmeticIntrinsics.Operators.LeftShift, new[] { OpCodes.Shl } },
            { ArithmeticIntrinsics.Operators.RightShift, new[] { OpCodes.Shr_Un } }
        };

        private SelectedInstructions<CilCodegenInstruction> SelectInstructionsAndWrap(
            Instruction instruction,
            ValueTag instructionTag,
            BasicBlockTag blockTag,
            LinkedListNode<ValueTag> insertionPoint,
            FlowGraph graph,
            HashSet<ValueTag> uninlineableValues = null)
        {
            var wrapper = new InstructionWrapper(
                this,
                instruction,
                instructionTag,
                blockTag,
                insertionPoint,
                graph,
                uninlineableValues ?? new HashSet<ValueTag>());

            return wrapper.SelectAndWrap();
        }

        /// <summary>
        /// Tries to move an instruction from its
        /// current location to just before another instruction.
        /// </summary>
        /// <param name="instruction">
        /// The instruction to try and move.
        /// </param>
        /// <param name="insertionPoint">
        /// An instruction to which the instruction should
        /// be moved. It must be defined in the same basic
        /// block as <paramref name="instruction"/>.
        /// </param>
        /// <param name="graph">
        /// The graph that defines both instructions.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="instruction"/> was
        /// successfully reordered; otherwise, <c>false</c>.
        /// </returns>
        private bool TryReorder(
            ValueTag instruction,
            LinkedListNode<ValueTag> insertionPoint,
            FlowGraph graph)
        {
            // Grab the ordering to which we should adhere.
            var ordering = graph.GetAnalysisResult<InstructionOrdering>();

            // Start at the linked list node belonging to the instruction
            // to move and work our way toward the insertion point.
            // Check the must-run-before relation as we traverse the list.
            var instructionNode = GetInstructionNode(instruction, graph);
            var currentNode = instructionNode;
            while (currentNode != insertionPoint)
            {
                if (ordering.MustRunBefore(instruction, currentNode.Value))
                {
                    // Aw snap, we encountered a dependency.
                    // Time to abandon ship.
                    return false;
                }

                currentNode = currentNode.Next;
            }

            // Looks like we can reorder the instruction!
            if (insertionPoint != instructionNode)
            {
                insertionPoint.List.Remove(instructionNode);
                insertionPoint.List.AddBefore(insertionPoint, instructionNode);
            }
            return true;
        }

        /// <summary>
        /// Gets the linked list node of an instruction in
        /// the instruction list of the instruction's block.
        /// </summary>
        /// <param name="instruction">
        /// The tag of the instruction whose node should be found.
        /// </param>
        /// <param name="graph">
        /// The graph that defines the instruction.
        /// </param>
        /// <returns>
        /// A linked list node.
        /// </returns>
        private LinkedListNode<ValueTag> GetInstructionNode(
            ValueTag instruction,
            FlowGraph graph)
        {
            var block = graph.GetInstruction(instruction).Block;
            return GetInstructionList(block).Find(instruction);
        }

        /// <summary>
        /// Gets the linked list of (reordered) instructions
        /// in a basic block.
        /// </summary>
        /// <param name="block">
        /// The block to find a list of (reordered) instructions for.
        /// </param>
        /// <returns>
        /// A linked list of value tags referring to instructions.
        /// </returns>
        private LinkedList<ValueTag> GetInstructionList(
            BasicBlock block)
        {
            LinkedList<ValueTag> blockInstructionList;
            if (!instructionOrder.TryGetValue(block.Tag, out blockInstructionList))
            {
                blockInstructionList = new LinkedList<ValueTag>(block.InstructionTags);
                blockInstructionList.AddLast(new LinkedListNode<ValueTag>(null));
                instructionOrder[block.Tag] = blockInstructionList;
            }
            return blockInstructionList;
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
                var sconst = (StringConstant)constant;
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
            return CreateSelection(instruction, (IReadOnlyList<ValueTag>)dependencies);
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateSelection(
            CilInstruction instruction,
            IReadOnlyList<ValueTag> dependencies)
        {
            return SelectedInstructions.Create<CilCodegenInstruction>(
                new CilCodegenInstruction[] { new CilOpInstruction(instruction) },
                dependencies);
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateSelection(
            OpCode instruction,
            IReadOnlyList<ValueTag> dependencies)
        {
            return CreateSelection(CilInstruction.Create(instruction), dependencies);
        }

        private static SelectedInstructions<CilCodegenInstruction> CreateNopSelection(
            IReadOnlyList<ValueTag> dependencies)
        {
            return SelectedInstructions.Create<CilCodegenInstruction>(
                EmptyArray<CilCodegenInstruction>.Value,
                dependencies);
        }

        /// <summary>
        /// A data structure that helps with wrapping selected instructions.
        /// </summary>
        private struct InstructionWrapper
        {
            public InstructionWrapper(
                CilInstructionSelector instructionSelector,
                Instruction instruction,
                ValueTag instructionTag,
                BasicBlockTag blockTag,
                LinkedListNode<ValueTag> insertionPoint,
                FlowGraph graph,
                HashSet<ValueTag> uninlineableValues)
            {
                this.InstructionSelector = instructionSelector;
                this.instruction = instruction;
                this.instructionTag = instructionTag;
                this.blockTag = blockTag;
                this.insertionPoint = insertionPoint;
                this.graph = graph;
                this.uninlineableValues = uninlineableValues;
                this.dependencyWorklist = null;
                this.dependencyArities = null;
                this.updatedInsns = null;
                this.updatedDependencies = null;
                this.uses = graph.GetAnalysisResult<ValueUses>();
            }

            public CilInstructionSelector InstructionSelector { get; private set; }

            private Instruction instruction;
            private ValueTag instructionTag;
            private BasicBlockTag blockTag;
            private LinkedListNode<ValueTag> insertionPoint;
            private FlowGraph graph;
            private HashSet<ValueTag> uninlineableValues;
            private Stack<ValueTag> dependencyWorklist;
            private Dictionary<ValueTag, int> dependencyArities;
            private ValueUses uses;
            private List<CilCodegenInstruction> updatedInsns;
            private List<ValueTag> updatedDependencies;

            /// <summary>
            /// Selects instructions and wraps them in dependency-loading +
            /// result-saving instructions.
            /// </summary>
            /// <returns>Selected and wrapped instructions.</returns>
            public SelectedInstructions<CilCodegenInstruction> SelectAndWrap()
            {
                var impl = InstructionSelector.SelectInstructionsImpl(instruction, graph);

                updatedInsns = new List<CilCodegenInstruction>();
                updatedDependencies = new List<ValueTag>();

                // Load or select each dependency.
                dependencyWorklist = new Stack<ValueTag>(impl.Dependencies);
                dependencyArities = impl.Dependencies
                    .GroupBy(tag => tag)
                    .ToDictionary(group => group.Key, group => group.Count());

                while (dependencyWorklist.Count > 0)
                {
                    LoadDependency(dependencyWorklist.Pop());
                }
                updatedInsns.Reverse();
                updatedDependencies.Reverse();

                // Actually run the instructions.
                updatedInsns.AddRange(impl.Instructions);

                // Store the result if it's not a `void` value.
                if (instruction.ResultType != InstructionSelector.TypeEnvironment.Void
                    && instructionTag != null)
                {
                    updatedInsns.Add(new CilStoreRegisterInstruction(instructionTag));
                }
                return SelectedInstructions.Create(updatedInsns, updatedDependencies);
            }

            private void LoadDependency(ValueTag dependency)
            {
                VariableDefinition allocaVarDef;
                if (InstructionSelector.AllocaToVariableMapping.TryGetValue(dependency, out allocaVarDef))
                {
                    // Replace references to `alloca` instructions that use
                    // local variables as backing storage with `ldloca` opcodes.
                    updatedInsns.Add(
                        new CilOpInstruction(
                            CilInstruction.Create(OpCodes.Ldloca,
                            allocaVarDef)));
                    return;
                }

                if (graph.ContainsInstruction(dependency))
                {
                    var dependencyImpl = graph.GetInstruction(dependency);
                    if (IsDefaultConstant(dependencyImpl.Instruction))
                    {
                        // Fall through to the default implementation.
                    }
                    else if (insertionPoint != null
                        && ShouldAlwaysInlineInstruction(dependencyImpl.Instruction))
                    {
                        // Some instructions should always be selected inline.
                        SelectDependencyInline(dependencyImpl);
                        return;
                    }
                    else if (insertionPoint != null
                        && uses.GetUseCount(dependency) == 1
                        && dependencyArities[dependency] == 1
                        && blockTag == dependencyImpl.Block.Tag
                        && !uninlineableValues.Contains(dependency)
                        && InstructionSelector.TryReorder(dependency, insertionPoint, graph))
                    {
                        // Selecting instructions inline allows us to keep values
                        // on the stack instead of pushing them into variables.
                        //
                        // However, we need to be really careful when doing so
                        // because it's easy to accidentally reorder instructions
                        // in a way that produces a non-equivalent program.
                        //
                        // These are the rules we'll adhere to:
                        //
                        //   1. Only same-block instructions that are used exactly
                        //      once are candidates for inline selection.
                        //
                        //   2. An instruction can only be selected inline if doing
                        //      so respects the must-run-before relation on instructions.
                        //      Specifically, a dependency can only be selected inline
                        //      if there is no instruction between the dependent instruction
                        //      and the new insertion point such that the dependency
                        //      must run before that instruction.
                        //
                        // We got this far, which means that the conditions above hold for
                        // the dependency. We can (actually, we kind of have to now) select
                        // the dependency inline.

                        SelectDependencyInline(dependencyImpl);
                        return;
                    }
                }

                // If all else fails, just insert a `load` instruction.
                if (graph.GetValueType(dependency) != InstructionSelector.TypeEnvironment.Void)
                {
                    updatedInsns.Add(new CilLoadRegisterInstruction(dependency));
                }
                updatedDependencies.Add(dependency);
            }

            private void SelectDependencyInline(SelectedInstruction dependency)
            {
                var dependencySelection = InstructionSelector.SelectInstructionsImpl(
                    dependency.Instruction,
                    graph);

                InstructionSelector.selectedInstructions.Add(dependency.Tag);
                updatedInsns.AddRange(dependencySelection.Instructions.Reverse());
                foreach (var subdependency in dependencySelection.Dependencies)
                {
                    dependencyWorklist.Push(subdependency);
                    int arity;
                    if (!dependencyArities.TryGetValue(subdependency, out arity))
                    {
                        arity = 0;
                    }
                    arity++;
                    dependencyArities[subdependency] = arity;
                }
                insertionPoint = insertionPoint.Previous;
            }

            private static bool ShouldAlwaysInlineInstruction(Instruction instruction)
            {
                var proto = instruction.Prototype as ConstantPrototype;
                return proto != null && proto.Value != DefaultConstant.Instance;
            }
        }

        private static bool IsDefaultConstant(Instruction instruction)
        {
            var proto = instruction.Prototype as ConstantPrototype;
            return proto != null && proto.Value == DefaultConstant.Instance;
        }
    }
}
